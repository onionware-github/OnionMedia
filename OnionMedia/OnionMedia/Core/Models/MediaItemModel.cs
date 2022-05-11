/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>
 */

using OnionMedia.Core.Classes;
using OnionMedia.Views.Dialogs;
using OnionMedia.Core.Enums;
using OnionMedia.Core.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using FFmpeg.NET;
using FFmpeg.NET.Events;
using FFMpegCore;
using CommunityToolkit.WinUI.Notifications;

namespace OnionMedia.Core.Models
{
    [ObservableObject]
    public sealed partial class MediaItemModel
    {
        private MediaItemModel(MediaFile mediaFile, IMediaAnalysis mediaAnalysis, byte[] hash)
        {
            this.hash = hash;
            MediaFile = mediaFile;
            MediaInfo = mediaAnalysis;
            VideoTimes = new TimeSpanGroup(mediaAnalysis.Duration);
            if (VideoStreamAvailable)
            {
                Width = (uint)MediaInfo.PrimaryVideoStream.Width;
                Height = (uint)MediaInfo.PrimaryVideoStream.Height;
                (AspectRatioWidth, AspectRatioHeight) = MediaInfo.PrimaryVideoStream.DisplayAspectRatio;
                FPS = MediaInfo.PrimaryVideoStream.FrameRate;
            }
            else if (AudioStreamAvailable) AudioOnly = true;

            //Hook events
            ffmpeg.Complete += ConversionDone;
            ffmpeg.Error += ConversionError;
            ffmpeg.Progress += ProgressChanged;
            ffmpeg.Data += (o, e) => Data?.Invoke(this, e);
            Cancel += ConversionCancelled;
            App.MainWindow.Closed += (o, e) => RaiseCancel();

            //Read tags
            try
            {
                FileTags = GetTags();
                FileTagsAvailable = true;
            }
            catch { FileTags = new(); }
        }

        private FileTags GetTags()
        {
            FileTags tags = new();
            var file = TagLib.File.Create(MediaFile.FileInfo.FullName);

            tags.Title = file.Tag.Title;
            tags.Artist = file.Tag.FirstPerformer;
            tags.Description = file.Tag.Description;
            tags.Album = file.Tag.Album;
            tags.Track = file.Tag.Track;
            tags.Year = file.Tag.Year;
            tags.Genre = file.Tag.FirstGenre;
            return tags;
        }

        private async void ConversionCancelled(object sender, CancellationEventArgs e)
        {
            if (!e.Restart)
            {
                await GlobalResources.DispatcherQueue.EnqueueAsync(() =>
                {
                    ConversionState = FFmpegConversionState.Cancelled;
                    ConversionProgress = 0;
                });
            }
        }

        private async void ProgressChanged(object sender, ConversionProgressEventArgs e)
        {
            if (!UnknownConversionProgress)
                await GlobalResources.DispatcherQueue.EnqueueAsync(() => ConversionProgress = e.ProcessedDuration / (VideoTimes.EndTime - VideoTimes.StartTime) * 100);
            Progress?.Invoke(this, e);
        }

        private async void ConversionDone(object sender, ConversionCompleteEventArgs e)
        {
            await GlobalResources.DispatcherQueue.EnqueueAsync(() =>
            {
                ConversionState = FFmpegConversionState.Done;
                ConversionProgress = 100;
            });
            Complete?.Invoke(this, e);
        }

        private async void ConversionError(object sender, ConversionErrorEventArgs e)
        {
            Debug.WriteLine(e.Exception);
            await GlobalResources.DispatcherQueue.EnqueueAsync(() =>
            {
                ConversionState = FFmpegConversionState.Failed;
                ConversionProgress = 0;
            });
            Error?.Invoke(this, e);
        }

        public static async Task<MediaItemModel> CreateAsync(FileInfo mediafile)
        {
            if (mediafile == null)
                throw new ArgumentNullException(nameof(mediafile));
            if (!File.Exists(mediafile.FullName))
                throw new FileNotFoundException("File does not exist.");

            byte[] hash;
            using (var md5 = MD5.Create())
            {
                using var stream = File.OpenRead(mediafile.FullName);
                hash = md5.ComputeHash(stream);
            }

            IMediaAnalysis mediaAnalysis = await FFProbe.AnalyseAsync(mediafile.FullName);
            return new MediaItemModel(new InputFile(mediafile.FullName), mediaAnalysis, hash);
        }

        private byte[] hash;
        private readonly Engine ffmpeg = new(GlobalResources.FFmpegPath);

        public async Task ConvertFileAsync(string filePath, ConversionPreset conversionArgs)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(MediaFile.FileInfo.FullName))
                throw new FileNotFoundException();

            using (var md5 = MD5.Create())
            {
                using var stream = File.OpenRead(MediaFile.FileInfo.FullName);
                if (!md5.ComputeHash(stream).SequenceEqual(hash))
                {
                    ConversionState = FFmpegConversionState.Failed;
                    throw new SecurityException("The file has been modified.");
                }
            }

            //Create a new temp directory
            string fileTempDir;
            do fileTempDir = GlobalResources.ConverterTempdir + $@"\{Path.GetRandomFileName()}";
            while (Directory.Exists(fileTempDir));
            Directory.CreateDirectory(fileTempDir);


            string extension = UseCustomOptions ? CustomOptions.Format.Name : conversionArgs.Format.Name;
            string fileTempPath = Path.Combine(fileTempDir, Path.ChangeExtension(Path.GetFileName(filePath), extension));

            ConversionState = FFmpegConversionState.Converting;
            try
            {
                Debug.WriteLine(BuildConversionArgs(fileTempPath, conversionArgs, out _));
                await ffmpeg.ExecuteAsync(BuildConversionArgs(fileTempPath, conversionArgs, out _), CancelSource.Token);
                if (ConversionState == FFmpegConversionState.Failed && AppSettings.Instance.FallBackToSoftwareEncoding && IsHardwareAcceleratedCodec(UseCustomOptions ? CustomOptions.VideoEncoder : conversionArgs.VideoEncoder))
                {
                    ConversionState = FFmpegConversionState.Converting;
                    await ffmpeg.ExecuteAsync(BuildConversionArgs(fileTempPath, conversionArgs, out _, true), CancelSource.Token);
                }

                if (ConversionState == FFmpegConversionState.Failed)
                    throw new Exception("Conversion failed.");
            }
            catch
            {
                if (Directory.Exists(fileTempDir))
                    try { Directory.Delete(fileTempDir, true); }
                    catch { /* Dont crash if the directory cant be deleted. */ }

                CancelSource.Token.ThrowIfCancellationRequested();
                throw;
            }

            //Make a filepath for the converted file and append the suffix from AppSettings.
            string dir = Path.GetDirectoryName(filePath);
            string name = Path.GetFileNameWithoutExtension(filePath) + AppSettings.Instance.ConvertedFilenameSuffix;
            string newFilePath = Path.Combine(dir, name + $".{extension}");

            //Add an underscore with a number when a file with the same path already exists.
            for (int i = 2; File.Exists(newFilePath); i++)
                newFilePath = Path.Combine(dir, name + $"_{i}" + $".{extension}");

            //Move the file to the desired directory.
            if (!File.Exists(fileTempPath)) return;
            Directory.CreateDirectory(dir);
            File.Move(fileTempPath, newFilePath);
            try { Directory.Delete(fileTempDir, true); }
            catch { /* Dont crash if the directory cant be deleted. */ }
        }

        public string BuildConversionArgs(string filePath, ConversionPreset conversionOptions, out string outputPath, bool forceSoftwareEncodedConversion = false)
        {
            if (filePath == null || conversionOptions == null)
                throw new ArgumentNullException(filePath == null ? nameof(filePath) : nameof(conversionOptions));

            string videoCodec = UseCustomOptions ? CustomOptions.VideoEncoder : conversionOptions.VideoEncoder;
            string audioCodec = UseCustomOptions ? CustomOptions.AudioEncoder : conversionOptions.AudioEncoder;
            if (videoCodec.IsNullOrWhiteSpace())
                videoCodec = UseCustomOptions ? CustomOptions.VideoCodec.Name : conversionOptions.VideoCodec.Name;
            if (audioCodec.IsNullOrWhiteSpace())
                audioCodec = UseCustomOptions ? CustomOptions.AudioCodec.Name : conversionOptions.AudioCodec.Name;

            if (forceSoftwareEncodedConversion)
                videoCodec = HardwareToSoftwareEncoder(videoCodec);

            StringBuilder argBuilder = new();
            argBuilder.Append($"-i \"{MediaFile.FileInfo.FullName}\" ");

            if (VideoStreamAvailable && ((conversionOptions.VideoAvailable && !UseCustomOptions) || CustomOptions.VideoAvailable && UseCustomOptions))
            {
                //Codec
                argBuilder.Append($"-codec:v {videoCodec} ");
                //Width and Height, Aspect Ratio
                if (Width > 1 && Height > 1 && (Width != MediaInfo.PrimaryVideoStream.Width || Height != MediaInfo.PrimaryVideoStream.Height))
                {
                    argBuilder.Append($"-vf scale={Width}x{Height}");
                    if (KeepAspectRatio)
                        argBuilder.Append($",setdar={Width}/{Height} ");
                    else
                        argBuilder.Append($",setdar={(AspectRatioWidth < 1 ? MediaInfo.PrimaryVideoStream.DisplayAspectRatio.Width : AspectRatioWidth)}/{(AspectRatioHeight < 1 ? MediaInfo.PrimaryVideoStream.DisplayAspectRatio.Height : AspectRatioHeight)} ");
                }
                //Bitrate
                if (VideoBitrate > 0)
                    argBuilder.Append($"-b:v {VideoBitrate} ");
                else if (IsHardwareAcceleratedCodec(videoCodec))
                    argBuilder.Append($"-b:v {MediaInfo.PrimaryVideoStream.BitRate} ");
                //Framerate
                if (FPS > 0 && FPS != MediaInfo.PrimaryVideoStream.FrameRate)
                    argBuilder.Append($"-r {FPS.ToString().Replace(',', '.')} ");
            }
            if (AudioStreamAvailable && ((conversionOptions.AudioAvailable && !UseCustomOptions) || CustomOptions.AudioAvailable && UseCustomOptions))
            {
                //TODO: Einstellung hinzufügen, um auszuwählen, was beim selben Codec passieren soll (Recoden oder ignorieren)
                if (MediaInfo.PrimaryAudioStream.CodecName != (UseCustomOptions ? CustomOptions.AudioEncoder : conversionOptions.AudioEncoder))
                    argBuilder.Append($"-codec:a {audioCodec} ");
                else
                    argBuilder.Append("-acodec copy ");
                if (AudioBitrate > 0)
                    argBuilder.Append($"-b:a {AudioBitrate} ");

                //Remove video when video is deactivated
                if ((!conversionOptions.VideoAvailable && !UseCustomOptions) || !CustomOptions.VideoAvailable && UseCustomOptions)
                    argBuilder.Append("-vn ");
            }

            if (!VideoTimes.StartTime.Equals(TimeSpan.Zero) || !VideoTimes.EndTime.Equals(VideoTimes.Duration))
                argBuilder.Append($"-ss {VideoTimes.StartTime} -to {VideoTimes.EndTime} ");

            if (!AppSettings.Instance.AutoSelectThreadsForConversion)
                argBuilder.Append($"-threads {AppSettings.Instance.MaxThreadCountForConversion} ");

            outputPath = Path.ChangeExtension(filePath, UseCustomOptions ? CustomOptions.Format.Name : conversionOptions.Format.Name);
            argBuilder.Append($"\"{outputPath}\"");
            return argBuilder.ToString();
        }

        /// <summary>
        /// Returns the software-encoded equivalent of the codec (e.g. h264_nvenc => libx264) or the codec itself if it already is a softwarecodec.
        /// </summary>
        /// <returns>The software-encoded equivalent of the codec.</returns>
        private static string HardwareToSoftwareEncoder(string hardwareEncoder) => hardwareEncoder switch
        {
            "h264_nvenc" or "h264_qsv" or "h264_amf" => "libx264",
            "hevc_nvenc" or "hevc_qsv" or "hevc_amf" => "libx265",
            _ => hardwareEncoder
        };

        private static bool IsHardwareAcceleratedCodec(string videoCodec)
            => videoCodec is "h264_nvenc" or "h264_qsv" or "h264_amf"
            or "hevc_nvenc" or "hevc_qsv" or "hevc_amf";

        public async Task ShowToastAsync(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException();

            Engine ffmpeg = new(GlobalResources.FFmpegPath);
            string tempThumbnailPath = Path.GetTempFileName() + ".jpg";
            await ffmpeg.GetThumbnailAsync(new InputFile(filePath), new OutputFile(tempThumbnailPath), CancellationToken.None);
            new ToastContentBuilder()
                            .AddText("conversionDone".GetLocalized("Resources"))
                            .AddText(Title)
                            .AddText(FileTags.Artist)
                            .AddInlineImage(new Uri(tempThumbnailPath))
                            .AddButton(new ToastButton()
                            .SetContent("playFile".GetLocalized("Resources"))
                            .AddArgument("action", "play")
                            .AddArgument("filepath", filePath)
                            .SetBackgroundActivation())
                            .AddButton(new ToastButton()
                            .SetContent("openFolder".GetLocalized("Resources"))
                            .AddArgument("action", "open path")
                            .AddArgument("folderpath", filePath))
                            .AddArgument("filenames", Path.GetFileName(filePath))
                            .SetBackgroundActivation()
                            .Show(toast =>
                            {
                                toast.Group = "conversionMsgs";
                                toast.Tag = "0";
                            });
        }

        public MediaFile MediaFile { get; }
        public bool AudioOnly { get; }
        public TimeSpanGroup VideoTimes { get; }
        public IMediaAnalysis MediaInfo { get; }

        public bool FileTagsAvailable { get; }
        public FileTags FileTags { get; private set; } = new();

        public bool ApplyNewTags(FileTags fileTags)
        {
            if (fileTags == null)
                throw new ArgumentNullException(nameof(fileTags));

            if (!File.Exists(MediaFile.FileInfo.FullName))
                throw new FileNotFoundException();

            using var md5 = MD5.Create();
            using (var stream = File.OpenRead(MediaFile.FileInfo.FullName))
                if (!md5.ComputeHash(stream).SequenceEqual(hash))
                    throw new SecurityException("The file has been modified.");

            var file = TagLib.File.Create(MediaFile.FileInfo.FullName);
            file.Tag.Title = fileTags.Title;
            file.Tag.Description = fileTags.Description;
            file.Tag.Performers = new[] { fileTags.Artist };
            file.Tag.Album = fileTags.Album;
            file.Tag.Genres = new[] { fileTags.Genre };
            file.Tag.Track = fileTags.Track;
            file.Tag.Year = fileTags.Year;
            file.Save();

            //Return false when the tags could not be edited.
            if (!fileTags.EqualsTagsFrom(GetTags()))
                return false;

            FileTags = fileTags;
            using (var stream = File.OpenRead(MediaFile.FileInfo.FullName))
                hash = md5.ComputeHash(stream);
            OnPropertyChanged(nameof(Title));
            return true;
        }

        public string Title => FileTags.Title.IsNullOrWhiteSpace() ? Path.GetFileNameWithoutExtension(MediaFile.FileInfo.FullName) : FileTags.Title;

        public bool VideoStreamAvailable => MediaInfo.PrimaryVideoStream != null;

        public bool AudioStreamAvailable => MediaInfo.PrimaryAudioStream != null;


        public bool UseCustomOptions
        {
            get => useCustomOptions;
            private set => SetProperty(ref useCustomOptions, value);
        }
        private bool useCustomOptions;

        public ConversionPreset CustomOptions
        {
            get => customOptions;
            private set => customOptions = value ?? throw new ArgumentNullException(nameof(value));
        }
        private ConversionPreset customOptions = new();

        public uint Width { get; set; }

        public uint Height { get; set; }

        public double FPS { get; set; }

        public int AspectRatioWidth { get; set; }

        public int AspectRatioHeight { get; set; }

        public bool KeepAspectRatio { get; set; } = true;


        [ObservableProperty]
        private long audioBitrate;

        [ObservableProperty]
        private long videoBitrate;

        [ObservableProperty]
        private FFmpegConversionState conversionState;

        [ObservableProperty]
        private double conversionProgress;

        public bool UnknownConversionProgress => MediaInfo.Duration.Equals(TimeSpan.Zero);

        public event EventHandler<ConversionDataEventArgs> Data;
        public event EventHandler<ConversionCompleteEventArgs> Complete;
        public event EventHandler<ConversionErrorEventArgs> Error;
        public event EventHandler<ConversionProgressEventArgs> Progress;
        public event EventHandler<CancellationEventArgs> Cancel;

        public CancellationTokenSource CancelSource { get; private set; } = new();
        public void RaiseCancel(bool restart = false)
        {
            if (!restart)
                CancelSource.Cancel();
            //TODO: Option to restart the download
            Cancel?.Invoke(this, new CancellationEventArgs(CancelSource, restart));
        }

        public void Uncancel()
        {
            if (CancelSource == null || CancelSource.IsCancellationRequested)
                CancelSource = new();
            ConversionState = FFmpegConversionState.None;
        }

        [ICommand]
        private async Task EditSpecificConversionOptions()
        {
            CustomPresetSelectorDialog dlg = UseCustomOptions ? new(CustomOptions) { XamlRoot = GlobalResources.XamlRoot } : new() { XamlRoot = GlobalResources.XamlRoot };
            if (await dlg.ShowAsync() == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                CustomOptions = dlg.ConversionPreset;
                UseCustomOptions = dlg.UseCustomOptions;
            }
            OnPropertyChanged(nameof(UseCustomOptions));
        }
    }
}

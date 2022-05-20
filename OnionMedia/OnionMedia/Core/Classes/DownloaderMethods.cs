/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using FFmpeg.NET;
using FFmpeg.NET.Enums;
using OnionMedia.Core.Extensions;
using OnionMedia.Core.Models;
using System.Security;
using System.Net.Http;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using YoutubeDLSharp.Metadata;
using YoutubeExplode.Exceptions;
using System.Text;
using CommunityToolkit.WinUI;
using System.Drawing;

namespace OnionMedia.Core.Classes
{
    public static class DownloaderMethods
    {
        public static readonly YoutubeClient youtube = new();

        public static readonly YoutubeDL downloadClient = new(5)
        {
            FFmpegPath = GlobalResources.FFmpegPath,
            YoutubeDLPath = GlobalResources.YtDlPath,
            OutputFolder = GlobalResources.DownloaderTempdir,
            OverwriteFiles = true
        };

        public static async Task DownloadStreamAsync(StreamItemModel stream, bool getMP4, YoutubeDL externalYoutubeClient = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            //   if (stream.ProgressInfo.IsCancelledOrFailed)
            //       return;

            YoutubeDL youtubeClient = externalYoutubeClient ?? downloadClient;
            OptionSet ytOptions = new() { RestrictFilenames = true };

            //Creates a temp directory if it does not already exist.
            Directory.CreateDirectory(GlobalResources.DownloaderTempdir);

            //Creates a new temp directory for this file
            string videotempdir;
            do videotempdir = GlobalResources.DownloaderTempdir + $@"\{Path.GetRandomFileName()}";
            while (Directory.Exists(videotempdir));
            Directory.CreateDirectory(videotempdir);

            stream.ProgressInfo.IsDone = false;
            stream.Converting = false;
            stream.DownloadState = Enums.DownloadState.IsLoading;
            stream.ConversionProgress = 0;
            stream.CancelEventHandler += CancelDownload;
            var cToken = stream.CancelSource.Token;

            bool autoConvertToH264 = AppSettings.Instance.AutoConvertToH264AfterDownload;

            string tempfile = string.Empty;
            bool isShortened = false;
            TimeSpan overhead = getMP4 ? (stream.TimeSpanGroup.StartTime.WithoutMilliseconds() < TimeSpan.FromSeconds(10) ? stream.TimeSpanGroup.StartTime : TimeSpan.FromSeconds(10)) : TimeSpan.Zero;
            Enums.ItemType itemType = Enums.ItemType.video;
        Start:
            try
            {
                Debug.WriteLine($"Current State of Progress: {stream.ProgressInfo.Progress}");
                ytOptions.Output = $@"{videotempdir}\%(id)s.%(ext)s";

                if (!stream.TimeSpanGroup.StartTime.Equals(TimeSpan.Zero) || !stream.TimeSpanGroup.EndTime.Equals(stream.Duration))
                {
                    ytOptions.ExternalDownloader = "ffmpeg";
                    ytOptions.ExternalDownloaderArgs = $"ffmpeg_i: -ss {stream.TimeSpanGroup.StartTime.WithoutMilliseconds() - overhead:hh\\:mm\\:ss}.00 -to {stream.TimeSpanGroup.EndTime:hh\\:mm\\:ss}.00";
                    isShortened = stream.TimeSpanGroup.StartTime.WithoutMilliseconds().Ticks > 0;
                    Debug.WriteLine(ytOptions.ExternalDownloaderArgs);
                }
                if (AppSettings.Instance.LimitDownloadSpeed && AppSettings.Instance.MaxDownloadSpeed > 0)
                    ytOptions.LimitRate = (long)(AppSettings.Instance.MaxDownloadSpeed * 1000000 / 8);
                stream.Downloading = true;

                //Audiodownload
                if (!getMP4)
                {
                    if (AppSettings.Instance.DownloadsAudioFormat != AudioConversionFormat.Opus)
                        ytOptions.AddCustomOption("-S", "ext");

                    RunResult<string> result = await youtubeClient.RunAudioDownload(stream.Video.Url, AudioConversionFormat.Best, cToken, stream.DownloadProgress, overrideOptions: ytOptions);
                    stream.Downloading = false;
                    if (result.Data.IsNullOrEmpty())
                        throw new Exception("Download failed!");
                    tempfile = result.Data;

                    if (isShortened)
                    {
                        var meta = await new Engine(GlobalResources.FFmpegPath).GetMetaDataAsync(new InputFile(tempfile), cToken);
                        overhead += meta.Duration - (stream.TimeSpanGroup.EndTime.WithoutMilliseconds() - (stream.TimeSpanGroup.StartTime.WithoutMilliseconds() - overhead));
                    }

                    string format = AppSettings.Instance.DownloadsAudioFormat.ToString().ToLower();
                    if (AppSettings.Instance.DownloadsAudioFormat is AudioConversionFormat.Vorbis)
                        format = "ogg";

                    if (isShortened || !tempfile.EndsWith(format))
                    {
                        stream.Converting = true;
                        tempfile = await ConvertAudioAsync(tempfile, format, overhead, stream, cToken);
                    }

                    Debug.WriteLine(tempfile);
                    stream.ProgressInfo.Progress = 100;
                    itemType = Enums.ItemType.audio;
                }
                //Videodownload
                else
                {
                    DownloadMergeFormat videoMergeFormat = (autoConvertToH264 || isShortened) ? DownloadMergeFormat.Unspecified : DownloadMergeFormat.Mp4;
                    VideoRecodeFormat videoRecodeFormat = (autoConvertToH264 || isShortened) ? VideoRecodeFormat.None : VideoRecodeFormat.Mp4;
                    ytOptions.AddCustomOption("--format-sort", "hdr:SDR");

                    Size originalSize = default;
                    FormatData formatData = null;

                    if (stream.CustomTimes && stream.QualityLabel.IsNullOrEmpty())
                    {
                        //Find the best NonDASH Preset
                        ytOptions.AddCustomOption("-S", "proto,hdr:SDR");
                    }
                    else if (stream.CustomTimes)
                    {
                        //Get formats that dont use DASH
                        var formats = stream.FormatQualityLabels.Where(f => f.Key.Protocol != "http_dash_segments" && f.Key.Extension != "3gp" && f.Key.HDR == "SDR");
                        formatData = formats.LastOrDefault(f => stream.QualityLabel.StartsWith(f.Value.ToString())).Key;
                        if (formatData == null)
                        {
                            int selectedHeight = int.Parse(stream.QualityLabel.TrimEnd('p'));
                            var higherFormats = formats.Where(f => f.Value > selectedHeight);
                            if (higherFormats.Any())
                            {
                                //TODO: Downscale to the selected resolution (Check for same aspect ratio)
                                int min = higherFormats.Min(f => f.Value);
                                formatData = higherFormats.Where(f => f.Value == min).Last().Key;

                                var defaultSize = stream.FormatQualityLabels.LastOrDefault(f => stream.QualityLabel.StartsWith(f.Value.ToString())).Key;
                                if (defaultSize != null)
                                    originalSize = new Size((int)defaultSize.Width, (int)defaultSize.Height);
                            }
                            else
                            {
                                //Find the best NonDASH format
                                ytOptions.AddCustomOption("-S", "proto,hdr:SDR");
                            }
                        }
                    }

                    string formatString;
                    if (formatData?.FormatId != null)
                        formatString = $"{formatData.FormatId}{(stream.Video.Formats.Any(f => f.VideoBitrate == null && f.AudioBitrate != null) ? "+bestaudio" : string.Empty)}";
                    else
                        formatString = stream.QualityLabel.IsNullOrEmpty() ? "bestvideo+bestaudio/best" : stream.Format;

                    RunResult<string> result = await youtubeClient.RunVideoDownload(stream.Video.Url, formatString, videoMergeFormat, videoRecodeFormat, cToken, stream.DownloadProgress, overrideOptions: ytOptions);
                    stream.Downloading = false;
                    if (result.Data.IsNullOrEmpty())
                        throw new Exception("Download failed!");

                    tempfile = result.Data;
                    Debug.WriteLine(tempfile);
                    stream.ProgressInfo.Progress = 100;

                    if (autoConvertToH264 || Path.GetExtension(tempfile) != ".mp4" || isShortened)
                    {
                        stream.Converting = true;
                        if (isShortened)
                        {
                            var meta = await new Engine(GlobalResources.FFmpegPath).GetMetaDataAsync(new InputFile(tempfile), cToken);
                            overhead += meta.Duration - (stream.TimeSpanGroup.EndTime.WithoutMilliseconds() - (stream.TimeSpanGroup.StartTime.WithoutMilliseconds() - overhead));
                            Debug.WriteLine(overhead);
                        }
                        else
                            overhead = TimeSpan.Zero;

                        tempfile = await ConvertToMp4Async(tempfile, overhead, default, stream, originalSize, cToken);
                    }
                }
            }
            catch (Exception ex)
            {
                stream.Downloading = false;
                stream.Converting = false;
                switch (ex)
                {
                    default:
                        Debug.WriteLine(ex.Message);
                        stream.DownloadState = Enums.DownloadState.IsFailed;
                        stream.ProgressInfo.IsCancelledOrFailed = true;
                        return;

                    case TaskCanceledException:
                        Debug.WriteLine("The download has cancelled.");
                        stream.DownloadState = Enums.DownloadState.IsCancelled;
                        stream.ProgressInfo.IsCancelledOrFailed = true;
                        return;

                    case DownloadRestartedException:
                        //THIS DOESNT WORK AT YET!
                        Debug.WriteLine("The download has restarted.");
                        stream.ProgressInfo.Progress = 0;
                        stream.ProgressInfo.IsCancelledOrFailed = true;
                        goto Start;

                    case HttpRequestException:
                        Debug.WriteLine("No internet connection!");
                        stream.DownloadState = Enums.DownloadState.IsFailed;
                        stream.ProgressInfo.IsCancelledOrFailed = true;
                        return;

                    case SecurityException:
                        Debug.WriteLine("No access to the temp-path!");
                        stream.DownloadState = Enums.DownloadState.IsFailed;
                        stream.ProgressInfo.IsCancelledOrFailed = true;
                        return;
                }
            }

            stream.Converting = false;
            Debug.WriteLine($"Current State of Progress: {stream.ProgressInfo.Progress}");

            try
            {
                //Writes the tags into the file
                if (Path.GetExtension(tempfile) is ".mp3" or ".mp4" or ".m4a" or ".flac")
                    SaveTags(tempfile, stream.Video);

                //Moves the video in the correct directory
                stream.Path = MoveToDisk(tempfile, stream.Video.Title.TrimToFilename(), itemType);

                stream.RaiseFinished();
                stream.DownloadState = Enums.DownloadState.IsDone;
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    default:
                        Debug.WriteLine(ex.Message);
                        stream.DownloadState = Enums.DownloadState.IsFailed;
                        stream.ProgressInfo.IsCancelledOrFailed = true;
                        break;

                    case FileNotFoundException:
                        Debug.WriteLine("File not found!");
                        stream.DownloadState = Enums.DownloadState.IsFailed;
                        stream.ProgressInfo.IsCancelledOrFailed = true;
                        break;

                    case UnauthorizedAccessException:
                        Debug.WriteLine("No permissions to access the file.");
                        stream.DownloadState = Enums.DownloadState.IsFailed;
                        stream.ProgressInfo.IsCancelledOrFailed = true;
                        break;

                    case PathTooLongException:
                        Debug.WriteLine("Path is too long");
                        stream.DownloadState = Enums.DownloadState.IsFailed;
                        stream.ProgressInfo.IsCancelledOrFailed = true;
                        break;

                    case DirectoryNotFoundException:
                        Debug.WriteLine("Directory not found!");
                        stream.DownloadState = Enums.DownloadState.IsFailed;
                        stream.ProgressInfo.IsCancelledOrFailed = true;
                        break;
                }
            }
        }

        private static void CancelDownload(object sender, CancellationEventArgs e)
        {
            if (e.Restart)
                throw new DownloadRestartedException();
            e.CancelSource.Cancel();
        }

        //Saves the tags into the file
        public static void SaveTags(string filename, VideoData meta)
        {
            var tagfile = TagLib.File.Create(filename);

            tagfile.Tag.Title = meta.Title;

            if (!meta.Description.IsNullOrEmpty())
                tagfile.Tag.Description = meta.Description;

            if (!meta.Uploader.IsNullOrEmpty())
                tagfile.Tag.Performers = new[] { meta.Uploader };

            if (meta.UploadDate != null)
                tagfile.Tag.Year = (uint)meta.UploadDate.Value.Year;

            tagfile.Save();
        }

        /// <summary>
        /// Converts the downloaded audio file to the selected audio format (AppSettings) and/or removes the overhead a the start.
        /// </summary>
        /// <returns>The path of the converted file.</returns>
        private static async Task<string> ConvertAudioAsync(string inputfile, string format, TimeSpan startTime = default, StreamItemModel stream = null, CancellationToken cancellationToken = default)
        {
            StringBuilder argBuilder = new();
            if (!inputfile.EndsWith(format))
            {
                argBuilder.Append($"-y -loglevel \"repeat+info\" -i \"{inputfile}\" -movflags \"+faststart\" -vn ");
                argBuilder.Append(GetAudioConversionArgs());
            }
            argBuilder.Append($"-ss {startTime} ");

            string outputpath = Path.ChangeExtension(inputfile, format);
            for (int i = 2; File.Exists(outputpath); i++)
                outputpath = Path.ChangeExtension(outputpath, $"_{i}.{format}");
            argBuilder.Append(outputpath);

            Debug.WriteLine(argBuilder.ToString());
            Engine ffmpeg = new(GlobalResources.FFmpegPath);
            var meta = await ffmpeg.GetMetaDataAsync(new InputFile(inputfile), cancellationToken);

            if (stream != null)
            {
                ffmpeg.Complete += async (o, e) => await GlobalResources.DispatcherQueue.EnqueueAsync(() => stream.ConversionProgress = 100);
                ffmpeg.Error += async (o, e) => await GlobalResources.DispatcherQueue.EnqueueAsync(() => stream.ConversionProgress = 0);
                ffmpeg.Progress += async (o, e) => await GlobalResources.DispatcherQueue.EnqueueAsync(() => stream.ConversionProgress = (int)(e.ProcessedDuration / (meta.Duration - startTime) * 100));
            }

            await ffmpeg.ExecuteAsync(argBuilder.ToString(), cancellationToken);
            return outputpath;
        }

        /// <summary>
        /// Converts a file to MP4 (with H264 codec if enabled in settings) and returns the new path
        /// </summary>
        /// <returns>The path of the converted file</returns>
        private static async Task<string> ConvertToMp4Async(string inputfile, TimeSpan startTime = default, TimeSpan endTime = default, StreamItemModel stream = null, Size resolution = default, CancellationToken cancellationToken = default)
        {
            string outputpath = Path.ChangeExtension(inputfile, ".converted.mp4");
            for (int i = 2; File.Exists(outputpath); i++)
                outputpath = Path.ChangeExtension(inputfile, $".converted_{i}.mp4");

            var input = new InputFile(inputfile);
            var output = new OutputFile(outputpath);
            Engine ffmpeg = new(GlobalResources.FFmpegPath);

            var con = new ConversionOptions();
            if (AppSettings.Instance.AutoConvertToH264AfterDownload)
            {
                if (AppSettings.Instance.UseHardwareAcceleratedEncoding)
                {
                    con.VideoCodec = AppSettings.Instance.HardwareEncoder switch
                    {
                        HardwareEncoder.Intel_QSV => VideoCodec.h264_qsv,
                        HardwareEncoder.AMD_AMF => VideoCodec.h264_amf,
                        HardwareEncoder.Nvidia_NVENC => VideoCodec.h264_nvenc,
                        _ => VideoCodec.libx264
                    };

                    var meta = await ffmpeg.GetMetaDataAsync(input, cancellationToken);
                    if (meta.VideoData?.BitRateKbs != null && meta.VideoData?.BitRateKbs > 0)
                    {
                        con.VideoBitRate = meta.VideoData.BitRateKbs * 8 / 1000;
                    }
                    else
                    {
                        int audioBytes = meta.AudioData?.BitRateKbs / 1000 * (int)meta.Duration.TotalSeconds ?? 0;
                        int sizeWithoutAudio = (int)meta.FileInfo.Length - audioBytes;
                        con.VideoBitRate = sizeWithoutAudio / (int)meta.Duration.TotalSeconds * 8 / 1000;
                    }
                }
                else
                    con.VideoCodec = VideoCodec.libx264;
            }
            else
                con.VideoCodec = VideoCodec.Default;

            StringBuilder extraArgs = new();
            extraArgs.Append($"-ss {startTime}");
            if (endTime != default)
                extraArgs.Append($" -to {endTime}");
            if (resolution != default)
                extraArgs.Append($" -vf scale={resolution.Width}:{resolution.Height}");
            con.ExtraArguments = extraArgs.ToString();

            if (!AppSettings.Instance.AutoSelectThreadsForConversion)
                con.Threads = AppSettings.Instance.MaxThreadCountForConversion;

            bool error = false;
            if (stream != null)
            {
                ffmpeg.Complete += async (o, e) => await GlobalResources.DispatcherQueue.EnqueueAsync(() => stream.ConversionProgress = 100);
                ffmpeg.Error += async (o, e) => await GlobalResources.DispatcherQueue.EnqueueAsync(() =>
                {
                    stream.ConversionProgress = 0;
                    error = true;
                    Debug.WriteLine(e.Exception);
                });
                ffmpeg.Progress += async (o, e) => await GlobalResources.DispatcherQueue.EnqueueAsync(() =>
                {
                    var totalDuration = e.TotalDuration - startTime;
                    if (endTime != default)
                        totalDuration -= totalDuration - endTime;

                    stream.ConversionProgress = (int)(e.ProcessedDuration / totalDuration * 100);
                });
            }

            await ffmpeg.ConvertAsync(input, output, con, cancellationToken);

            //Fallback to software-encoding when hardware-encoding doen't work
            if (error && con.VideoCodec is VideoCodec.h264_nvenc or VideoCodec.h264_qsv or VideoCodec.h264_amf && AppSettings.Instance.FallBackToSoftwareEncoding)
            {
                error = false;
                con.VideoCodec = VideoCodec.libx264;
                con.VideoBitRate = null;
                await ffmpeg.ConvertAsync(input, output, con, cancellationToken);
            }

            if (error)
                stream.DownloadState = Enums.DownloadState.IsFailed;
            return outputpath;
        }


        private static Uri MoveToDisk(string tempfile, string videoname, Enums.ItemType itemType)
        {
            string savefilepath;
            string extension = Path.GetExtension(tempfile);

            if (videoname.IsNullOrEmpty())
                videoname = "Download";

            if (itemType == Enums.ItemType.audio)
                savefilepath = AppSettings.Instance.DownloadsAudioSavePath;
            else
                savefilepath = AppSettings.Instance.DownloadsVideoSavePath;

            Directory.CreateDirectory(savefilepath);
            string filename = @$"{savefilepath}\{videoname.TrimToFilename()}{extension}";

            if (!File.Exists(filename))
                File.Move(tempfile, filename);

            else
            {
                string dir = Path.GetDirectoryName(filename);
                string name = Path.GetFileNameWithoutExtension(filename);

                //Increases the number until a possible file name is found
                for (int i = 2; File.Exists(filename); i++)
                    filename = $@"{dir}\{name}_{i}{extension}";

                File.Move(tempfile, filename);
            }
            return new Uri(filename);
        }

        //Returns the resolutions from the videos.
        public static IEnumerable<string> GetResolutions(IEnumerable<StreamItemModel> videos)
        {
            ReadOnlySpan<int> validResolutions = stackalloc[] { 144, 144, 240, 360, 480, 720, 1080, 1440, 2160, 4320, 8640 };
            SortedSet<int> heights = new();
            SortedSet<int> correctHeights = new();
            List<string> resolutions = new();

            //Add video heights to a SortedSet
            foreach (var video in videos.Where(v => v != null))
                if (video.Video.Formats != null)
                    foreach (var format in video.Video.Formats.Where(f => f != null))
                        if (format.Height != null)
                            heights.Add((int)format.Height);

            //Round up "non-default" resolutions
            foreach (int height in heights)
            {
                for (int i = 1; i < validResolutions.Length; i++)
                {
                    if (height < validResolutions[i] && height > validResolutions[i - 1] || height == validResolutions[i])
                    {
                        correctHeights.Add(validResolutions[i]);
                        break;
                    }
                    else if (height < validResolutions[i])
                    {
                        correctHeights.Add(validResolutions[i - 1]);
                        break;
                    }
                }
            }

            //Converts the heights to string values and adds a "p" at the end.
            foreach (int height in correctHeights.Reverse())
                resolutions.Add(height + "p");

            return resolutions;
        }

        public static async IAsyncEnumerable<SearchItemModel> GetSearchResultsAsync(string searchTerm, int maxResults = 20)
        {
            if (VideoSearchCancelSource.IsCancellationRequested)
                VideoSearchCancelSource = new();

            int counter = 0;
            await foreach (var result in youtube.Search.GetVideosAsync(searchTerm, VideoSearchCancelSource.Token))
            {
                if (counter == maxResults)
                    break;

                yield return new SearchItemModel(result);
                counter++;
            }
        }
        public static CancellationTokenSource VideoSearchCancelSource { get; private set; } = new();


        private static string GetAudioConversionArgs() => AppSettings.Instance.DownloadsAudioFormat switch
        {
            AudioConversionFormat.Flac => "-acodec flac ",
            AudioConversionFormat.Opus => "-acodec libopus ",
            AudioConversionFormat.M4a => $"-acodec aac \"-bsf:a\" aac_adtstoasc -aq {GetAudioQuality("aac").ToString().Replace(',', '.')} ",
            AudioConversionFormat.Mp3 => $"-acodec libmp3lame -aq {GetAudioQuality("libmp3lame").ToString().Replace(',', '.')} ",
            AudioConversionFormat.Vorbis => $"-acodec libvorbis -aq {GetAudioQuality("libvorbis").ToString().Replace(',', '.')} ",
            _ => $"-f {AppSettings.Instance.DownloadsAudioFormat.ToString().ToLower()} "
        };

        private static readonly Dictionary<string, (float, int)> audioQualityDict = new()
        {
            { "libmp3lame", (10, 0) },
            { "libvorbis", (0, 10) },
            // FFmpeg's AAC encoder does not have an upper limit for the value of -aq.
            // Experimentally, with values over 4, bitrate changes were minimal or non-existent
            { "aac", (0.1f, 4) }
        };

        //Preferred quality for audio-conversion (up to 10).
        private const float preferredQuality = 5f;

        private static float GetAudioQuality(string codec)
            => audioQualityDict[codec].Item2 + (audioQualityDict[codec].Item1 - audioQualityDict[codec].Item2) * (preferredQuality / 10);
    }
}

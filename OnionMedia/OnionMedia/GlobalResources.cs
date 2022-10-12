/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using FFMpegCore;
using Microsoft.UI.Xaml;
using OnionMedia.Core.Extensions;
using OnionMedia.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using OnionMedia.Core;
using OnionMedia.Core.Services;
using YoutubeDLSharp.Options;

namespace OnionMedia
{
    internal static class GlobalResources
    {
        public static HardwareEncoder[] HardwareEncoders { get; } = Enum.GetValues<HardwareEncoder>();
        public static AudioConversionFormat[] AudioConversionFormats { get; } = Enum.GetValues<AudioConversionFormat>().Take(Range.StartAt(2)).ToArray();
        public static LibraryInfo[] LibraryLicenses { get; } =
        {
            new LibraryInfo("FFmpeg", "FFmpeg 64-bit static Windows build from www.gyan.dev", "GNU GPL v3", Installpath + @"\ExternalBinaries\ffmpeg+yt-dlp\FFmpeg_LICENSE", "https://github.com/FFmpeg/FFmpeg/commit/9687cae2b4"),
            new LibraryInfo("yt-dlp", "yt-dlp", "Unlicense", LicensesDir + "yt-dlp.txt", "https://github.com/yt-dlp/yt-dlp"),
            new LibraryInfo("CommunityToolkit", "Microsoft", "MIT License", LicensesDir + "communitytoolkit.txt", "https://github.com/CommunityToolkit/WindowsCommunityToolkit"),
            new LibraryInfo("FFMpegCore", "Vlad Jerca", "MIT License", LicensesDir + "FFMpegCore.txt", "https://github.com/rosenbjerg/FFMpegCore"),
            new LibraryInfo("Newtonsoft.Json", "James Newton-King", "MIT License", LicensesDir + "newtonsoft_json.txt", "https://github.com/JamesNK/Newtonsoft.Json"),
            new LibraryInfo("TagLib#", "mono", "LGPL v2.1", LicensesDir + "TagLibSharp.txt", "https://github.com/mono/taglib-sharp"),
            new LibraryInfo("xFFmpeg.NET", "Tobias Haimerl(cmxl)", "MIT License", LicensesDir + "xFFmpeg.NET.txt", "https://github.com/cmxl/FFmpeg.NET"),
            new LibraryInfo("XamlBehaviors", "Microsoft", "MIT License", LicensesDir + "microsoft_mit_license.txt", "https://github.com/Microsoft/XamlBehaviors"),
            new LibraryInfo("YoutubeDLSharp", "Bluegrams", "BSD 3-Clause License", LicensesDir + "YoutubeDLSharp.txt", "https://github.com/Bluegrams/YoutubeDLSharp"),
            new LibraryInfo("YoutubeExplode", "Tyrrrz", "LGPL v3", LicensesDir + "YoutubeExplode.txt", "https://github.com/Tyrrrz/YoutubeExplode")
        };

        public static string Installpath => Windows.Application­Model.Package.Current.Installed­Location.Path;
        public static string LocalPath => ApplicationData.Current.LocalFolder.Path;
        public static string Tempdir => Path.GetTempPath() + @"\Onionmedia";
        public static string ConverterTempdir => Tempdir + @"\Converter";
        public static string DownloaderTempdir => Tempdir + @"\Downloader";
        public static string FFmpegPath => Installpath + @"\ExternalBinaries\ffmpeg+yt-dlp\binaries\ffmpeg.exe";
        public static string YtDlPath => Installpath + @"\ExternalBinaries\ffmpeg+yt-dlp\binaries\yt-dlp.exe";
        public static string LicensesDir => Installpath + @"\licenses\";
        public static int SystemThreadCount => Environment.ProcessorCount;
        public static FFmpegCodecConfig FFmpegCodecs { get; set; }
        public static XamlRoot XamlRoot { get; set; }

        public const string INVALIDFILENAMECHARACTERSREGEX = @"[<|>:""/\?*]";
        public const string FFMPEGTIMEFROMOUTPUTREGEX = "time=[0-9]{2}:[0-9]{2}:[0-9]{2}.[0-9]{2}";
        public const string URLREGEX = @"^(?:https?:\/\/)?(?:www[.])?\S+[.]\S+(?:[\/]+\S*)*$";
        private const string DialogResources = "DialogResources";


        //Shared methods
        public static async Task DisplayFileSaveErrorDialog(uint unauthorizedAccessExceptions, uint directoryNotFoundExceptions, uint notEnoughSpaceExceptions)
        {
            var dialogService = IoC.Default.GetService<IDialogService>();
            DialogTextOptions dlgConfig = new()
            {
                CloseButtonText = "OK",
                ContentTextWrapping = TextWrapMode.WrapWholeWords
            };

            if (MultipleExceptionTypes(unauthorizedAccessExceptions, directoryNotFoundExceptions, notEnoughSpaceExceptions))
            {
                dlgConfig.Title = "conversionFilesCantBeSavedTitle".GetLocalized(DialogResources);
                dlgConfig.Content = "conversionFilesCantBeSaved".GetLocalized(DialogResources).Replace("{0}", (unauthorizedAccessExceptions + directoryNotFoundExceptions).ToString());
                await dialogService.ShowDialogAsync(dlgConfig);
                return;
            }
            if (unauthorizedAccessExceptions > 0)
            {
                dlgConfig.Title = "conversionFilesNoWriteAccessTitle".GetLocalized(DialogResources);
                dlgConfig.Content = "conversionFilesNoWriteAccess".GetLocalized(DialogResources).Replace("{0}", unauthorizedAccessExceptions.ToString());
                await dialogService.ShowDialogAsync(dlgConfig);
                return;
            }
            if (directoryNotFoundExceptions > 0)
            {
                dlgConfig.Title = "conversionFilesPathNotFoundTitle".GetLocalized(DialogResources);
                dlgConfig.Content = "conversionFilesPathNotFound".GetLocalized(DialogResources).Replace("{0}", directoryNotFoundExceptions.ToString());
                await dialogService.ShowDialogAsync(dlgConfig);
                return;
            }
            if (notEnoughSpaceExceptions > 0)
            {
                dlgConfig.Title = "notEnoughSpaceTitle".GetLocalized(DialogResources);
                dlgConfig.Content = "notEnoughSpace".GetLocalized(DialogResources).Replace("{0}", notEnoughSpaceExceptions.ToString());
                await dialogService.ShowDialogAsync(dlgConfig);
                return;
            }
        }

        private static bool MultipleExceptionTypes(params uint[] amountOfException) => amountOfException != null && amountOfException.Count(n => n > 0) > 1;

        public static long CalculateVideoBitrate(string filepath, IMediaAnalysis meta)
        {
            if (filepath == null || meta == null)
                throw new ArgumentNullException(filepath == null ? nameof(filepath) : nameof(meta));
            if (!File.Exists(filepath))
                throw new FileNotFoundException();

            long audioBytes = meta.PrimaryAudioStream?.BitRate * (int)meta.Duration.TotalSeconds ?? 0;
            long sizeWithoutAudio = new FileInfo(filepath).Length - audioBytes;
            return sizeWithoutAudio / (int)meta.Duration.TotalSeconds * 8;
        }


        /// <summary>
        /// Moves a file to an another location. When the file at <paramref name="destFileName"/> already exists, it will be overwritten.
        /// </summary>
        /// <param name="sourceFileName">The file to move to <paramref name="destFileName"/>.</param>
        /// <param name="destFileName">The new name and location of <paramref name="sourceFileName"/>.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public static async Task MoveFileAsync(string sourceFileName, string destFileName, CancellationToken cancellationToken = default)
        {
            if (sourceFileName.IsNullOrEmpty() || destFileName.IsNullOrEmpty())
                throw new ArgumentNullException();

            if (!File.Exists(sourceFileName))
                throw new FileNotFoundException("Input file cannot be found.", sourceFileName);

            bool sameDrive = sourceFileName[0].Equals(destFileName[0]);
            if (sameDrive)
            {
                File.Move(sourceFileName, destFileName, true);
                return;
            }

            try
            {
                //If the filepaths points to different volumes, copy the file so that it can be canceled.
                using (FileStream input = new(sourceFileName, FileMode.Open))
                using (FileStream output = new(destFileName, FileMode.Create))
                    await input.CopyToAsync(output, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (File.Exists(destFileName))
                    File.Delete(destFileName);
                throw;
            }
            File.Delete(sourceFileName);
            await Task.CompletedTask;
        }

#if DEBUG
        public const bool IS_DEBUG = true;
#else
        public const bool IS_DEBUG = false;
#endif
    }
}

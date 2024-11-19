using FFMpegCore;
using OnionMedia.Core.Extensions;
using OnionMedia.Core.Models;
using OnionMedia.Core.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDLSharp.Options;

namespace OnionMedia.Core
{
    public static class GlobalResources
    {
        private static readonly IPathProvider pathProvider = IoC.Default.GetService<IPathProvider>() ?? throw new ArgumentNullException();

        public static HardwareEncoder[] HardwareEncoders { get; } = Enum.GetValues<HardwareEncoder>();
        public static AudioConversionFormat[] AudioConversionFormats { get; } = Enum.GetValues<AudioConversionFormat>().Take(Range.StartAt(2)).ToArray();

        public static LibraryInfo[] LibraryLicenses { get; } = IoC.Default.GetService<IDataCollectionProvider<LibraryInfo>>().GetItems();

        public static FFmpegCodecConfig FFmpegCodecs { get; set; }
        public const string INVALIDFILENAMECHARACTERSREGEX = @"[<|>:""/\?*]";
        public const string FFMPEGTIMEFROMOUTPUTREGEX = "time=[0-9]{2}:[0-9]{2}:[0-9]{2}.[0-9]{2}";
        public const string URLREGEX = @"^(?:https?:\/\/)?(?:www[.])?\S+[.]\S+(?:[\/]+\S*)*$";

        public static string LocalDonationUrl
        {
	        get
	        {
		        switch (CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol)
		        {
			        case "€":
				        return "https://www.paypal.com/donate/?hosted_button_id=5TABD3FZYH452";
                    case "CHF":
                        return "https://www.paypal.com/donate/?hosted_button_id=MN8ESWH82EENA";
                    case "£":
                        return "https://www.paypal.com/donate/?hosted_button_id=MYQV428WNNYX6";
					default: //$
				        return "https://www.paypal.com/donate/?hosted_button_id=D6H44EWFWZ5YE";
		        }
			}
        }


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

/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>
 */

using CommunityToolkit.Mvvm.ComponentModel;
using OnionMedia.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using YoutubeDLSharp.Options;

namespace OnionMedia.Core.Models
{
    public sealed class AppSettings : ObservableObject
    {
        private AppSettings()
        {
            simultaneousOperationCount = ApplicationData.Current.LocalSettings.Values["simultaneousOperationCount"] as int? ?? 3;
            limitDownloadSpeed = ApplicationData.Current.LocalSettings.Values["limitDownloadSpeed"] as bool? ?? false;
            maxDownloadSpeed = ApplicationData.Current.LocalSettings.Values["maxDownloadSpeed"] as double? ?? 10;
            clearListsAfterOperation = ApplicationData.Current.LocalSettings.Values["clearListsAfterOperation"] as bool? ?? false;
            autoConvertToH264AfterDownload = ApplicationData.Current.LocalSettings.Values["autoConvertToH264AfterDownload"] as bool? ?? false;
            useHardwareAcceleratedEncoding = ApplicationData.Current.LocalSettings.Values["useHardwareAcceleratedEncoding"] as bool? ?? false;
            //TODO: Check which encoders are available on the system
            hardwareEncoder = ParseEnum<HardwareEncoder>(ApplicationData.Current.LocalSettings.Values["hardwareEncoder"]);
            autoSelectThreadsForConversion = ApplicationData.Current.LocalSettings.Values["autoSelectThreadsForConversion"] as bool? ?? true;
            maxThreadCountForConversion = ApplicationData.Current.LocalSettings.Values["maxThreadCountForConversion"] as int? ?? 1;
            useFixedStoragePaths = ApplicationData.Current.LocalSettings.Values["useFixedStoragePaths"] as bool? ?? true;
            convertedAudioSavePath = ApplicationData.Current.LocalSettings.Values["convertedAudioSavePath"] as string ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "OnionMedia", "converted".GetLocalized());
            convertedVideoSavePath = ApplicationData.Current.LocalSettings.Values["convertedVideoSavePath"] as string ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "OnionMedia", "converted".GetLocalized());
            downloadsAudioSavePath = ApplicationData.Current.LocalSettings.Values["downloadsAudioSavePath"] as string ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "OnionMedia", "downloaded".GetLocalized());
            downloadsVideoSavePath = ApplicationData.Current.LocalSettings.Values["downloadsVideoSavePath"] as string ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "OnionMedia", "downloaded".GetLocalized());
            convertedFilenameSuffix = ApplicationData.Current.LocalSettings.Values["convertedFilenameSuffix"] as string ?? string.Empty;
            sendMessageAfterConversion = ApplicationData.Current.LocalSettings.Values["sendMessageAfterConversion"] as bool? ?? true;
            sendMessageAfterDownload = ApplicationData.Current.LocalSettings.Values["sendMessageAfterDownload"] as bool? ?? true;
            fallBackToSoftwareEncoding = ApplicationData.Current.LocalSettings.Values["fallBackToSoftwareEncoding"] as bool? ?? true;

            var downloadsAudioFormat = ApplicationData.Current.LocalSettings.Values["downloadsAudioFormat"];
            if (downloadsAudioFormat == null)
                this.downloadsAudioFormat = AudioConversionFormat.Mp3;
            else
                this.downloadsAudioFormat = ParseEnum<AudioConversionFormat>(downloadsAudioFormat);
        }

        public static AppSettings Instance { get; } = new AppSettings();

        //Settings
        public int SimultaneousOperationCount
        {
            get => simultaneousOperationCount.HasValue ? (int)simultaneousOperationCount : default;
            set => SetSetting(ref simultaneousOperationCount, value, "simultaneousOperationCount");
        }
        private int? simultaneousOperationCount;

        public bool LimitDownloadSpeed
        {
            get => limitDownloadSpeed.HasValue && (bool)limitDownloadSpeed;
            set => SetSetting(ref limitDownloadSpeed, value, "limitDownloadSpeed");
        }
        private bool? limitDownloadSpeed;

        /// <summary>
        /// The speed limit per download in MBit/s
        /// </summary>
        public double MaxDownloadSpeed
        {
            get => maxDownloadSpeed.HasValue ? (double)maxDownloadSpeed : default;
            set => SetSetting(ref maxDownloadSpeed, value, "maxDownloadSpeed");
        }
        private double? maxDownloadSpeed;

        public bool ClearListsAfterOperation
        {
            get => clearListsAfterOperation.HasValue && (bool)clearListsAfterOperation;
            set => SetSetting(ref clearListsAfterOperation, value, "clearListsAfterOperation");
        }
        private bool? clearListsAfterOperation;

        /// <summary>
        /// Fallback to software encoding if hardware-accelerated encoding fails.
        /// </summary>
        public bool FallBackToSoftwareEncoding
        {
            get => fallBackToSoftwareEncoding.HasValue && (bool)fallBackToSoftwareEncoding;
            set => SetSetting(ref fallBackToSoftwareEncoding, value, "fallBackToSoftwareEncoding");
        }
        private bool? fallBackToSoftwareEncoding;

        public bool AutoConvertToH264AfterDownload
        {
            get => autoConvertToH264AfterDownload.HasValue && (bool)autoConvertToH264AfterDownload;
            set => SetSetting(ref autoConvertToH264AfterDownload, value, "autoConvertToH264AfterDownload");
        }
        private bool? autoConvertToH264AfterDownload;

        public bool UseHardwareAcceleratedEncoding
        {
            get => useHardwareAcceleratedEncoding.HasValue && (bool)useHardwareAcceleratedEncoding;
            set => SetSetting(ref useHardwareAcceleratedEncoding, value, "useHardwareAcceleratedEncoding");
        }
        private bool? useHardwareAcceleratedEncoding;

        public bool SendMessageAfterConversion
        {
            get => sendMessageAfterConversion.HasValue && (bool)sendMessageAfterConversion;
            set => SetSetting(ref sendMessageAfterConversion, value, "sendMessageAfterConversion");
        }
        private bool? sendMessageAfterConversion;

        public bool SendMessageAfterDownload
        {
            get => sendMessageAfterDownload.HasValue && (bool)sendMessageAfterDownload;
            set => SetSetting(ref sendMessageAfterDownload, value, "sendMessageAfterDownload");
        }
        private bool? sendMessageAfterDownload;

        public HardwareEncoder HardwareEncoder
        {
            get => (HardwareEncoder)hardwareEncoder;
            set
            {
                if (SetProperty(ref hardwareEncoder, value))
                    ApplicationData.Current.LocalSettings.Values["hardwareEncoder"] = value.ToString();
            }
        }
        private HardwareEncoder? hardwareEncoder;

        public AudioConversionFormat DownloadsAudioFormat
        {
            get => downloadsAudioFormat;
            set
            {
                if (SetProperty(ref downloadsAudioFormat, value))
                    ApplicationData.Current.LocalSettings.Values["downloadsAudioFormat"] = value.ToString();
            }
        }
        private AudioConversionFormat downloadsAudioFormat;

        public bool AutoSelectThreadsForConversion
        {
            get => autoSelectThreadsForConversion.HasValue && (bool)autoSelectThreadsForConversion;
            set => SetSetting(ref autoSelectThreadsForConversion, value, "autoSelectThreadsForConversion");
        }
        private bool? autoSelectThreadsForConversion;

        public int MaxThreadCountForConversion
        {
            get => maxThreadCountForConversion.HasValue ? (int)maxThreadCountForConversion : default;
            set => SetSetting(ref maxThreadCountForConversion, value, "maxThreadCountForConversion");
        }
        private int? maxThreadCountForConversion;


        //Paths to save files
        public bool UseFixedStoragePaths
        {
            get => useFixedStoragePaths.HasValue && (bool)useFixedStoragePaths;
            set => SetSetting(ref useFixedStoragePaths, value, "useFixedStoragePaths");
        }
        private bool? useFixedStoragePaths;

        public string ConvertedVideoSavePath
        {
            get => convertedVideoSavePath;
            set => SetSetting(ref convertedVideoSavePath, value, "convertedVideoSavePath");
        }
        private string convertedVideoSavePath;

        public string ConvertedAudioSavePath
        {
            get => convertedAudioSavePath;
            set => SetSetting(ref convertedAudioSavePath, value, "convertedAudioSavePath");
        }
        private string convertedAudioSavePath;

        public string DownloadsVideoSavePath
        {
            get => downloadsVideoSavePath;
            set => SetSetting(ref downloadsVideoSavePath, value, "downloadsVideoSavePath");
        }
        private string downloadsVideoSavePath;

        public string DownloadsAudioSavePath
        {
            get => downloadsAudioSavePath;
            set => SetSetting(ref downloadsAudioSavePath, value, "downloadsAudioSavePath");
        }
        private string downloadsAudioSavePath;

        public string ConvertedFilenameSuffix
        {
            get => convertedFilenameSuffix;
            set => SetSetting(ref convertedFilenameSuffix, Regex.Replace(value, GlobalResources.INVALIDFILENAMECHARACTERSREGEX, string.Empty), "convertedFilenameSuffix", true);
        }
        private string convertedFilenameSuffix;

        //Last opened page
        public bool DownloaderPageIsOpen
        {
            get => ApplicationData.Current.LocalSettings.Values["downloaderPageIsOpen"] as bool? ?? false;
            set => ApplicationData.Current.LocalSettings.Values["downloaderPageIsOpen"] = value;
        }


        private void SetSetting<T>(ref T field, T value, string settingName, bool forceOnPropertyChanged = false, [CallerMemberName] string propName = null)
        {
            if (SetProperty(ref field, value, propName))
                ApplicationData.Current.LocalSettings.Values[settingName] = value;
            else if (forceOnPropertyChanged)
                OnPropertyChanged(propName);
        }

        private static T ParseEnum<T>(object value)
            => value != null ? (T)Enum.Parse(typeof(T), value.ToString()) : default;
    }

    public enum HardwareEncoder
    {
        [Display(Name = "Intel QuickSync Video")]
        Intel_QSV,
        [Display(Name = "AMD AMF")]
        AMD_AMF,
        [Display(Name = "NVIDIA NVENC")]
        Nvidia_NVENC
    }

    public enum PathType
    {
        ConvertedVideofiles,
        ConvertedAudiofiles,
        DownloadedVideofiles,
        DownloadedAudiofiles
    }
}

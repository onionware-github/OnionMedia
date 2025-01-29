﻿/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using OnionMedia.Core.Enums;
using OnionMedia.Core.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OnionMedia.Core.Services;
using YoutubeDLSharp.Options;

namespace OnionMedia.Core.Models
{
    public sealed class AppSettings : ObservableObject
    {
        private AppSettings()
        {
            simultaneousOperationCount = settingsService.GetSetting("simultaneousOperationCount") as int? ?? 3;
            ValidateSettingOrSetToDefault(ref simultaneousOperationCount, val => val is > 0 and <= 5, 3);
            
            limitDownloadSpeed = settingsService.GetSetting("limitDownloadSpeed") as bool? ?? false;
            maxDownloadSpeed = settingsService.GetSetting("maxDownloadSpeed") as double? ?? 10;
            ValidateSettingOrSetToDefault(ref maxDownloadSpeed, val => val is >= 0, 10);
            
            clearListsAfterOperation = settingsService.GetSetting("clearListsAfterOperation") as bool? ?? false;
            autoConvertToH264AfterDownload = settingsService.GetSetting("autoConvertToH264AfterDownload") as bool? ?? false;
            useHardwareAcceleratedEncoding = settingsService.GetSetting("useHardwareAcceleratedEncoding") as bool? ?? false;
            //TODO: Check which encoders are available on the system
            hardwareEncoder = ParseEnum<HardwareEncoder>(settingsService.GetSetting("hardwareEncoder"));
            autoSelectThreadsForConversion = settingsService.GetSetting("autoSelectThreadsForConversion") as bool? ?? true;
            maxThreadCountForConversion = settingsService.GetSetting("maxThreadCountForConversion") as int? ?? 1;
            ValidateSettingOrSetToDefault(ref maxThreadCountForConversion, val => val is > 0, 1);
            

            useFixedStoragePaths = settingsService.GetSetting("useFixedStoragePaths") as bool? ?? true;
            convertedAudioSavePath = settingsService.GetSetting("convertedAudioSavePath") as string ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "OnionMedia", "converted".GetLocalized());
            convertedVideoSavePath = settingsService.GetSetting("convertedVideoSavePath") as string ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "OnionMedia", "converted".GetLocalized());
            downloadsAudioSavePath = settingsService.GetSetting("downloadsAudioSavePath") as string ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "OnionMedia", "downloaded".GetLocalized());
            downloadsVideoSavePath = settingsService.GetSetting("downloadsVideoSavePath") as string ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "OnionMedia", "downloaded".GetLocalized());
            convertedFilenameSuffix = Regex.Replace(settingsService.GetSetting("convertedFilenameSuffix") as string ?? string.Empty, GlobalResources.INVALIDFILENAMECHARACTERSREGEX, string.Empty);
            sendMessageAfterConversion = settingsService.GetSetting("sendMessageAfterConversion") as bool? ?? true;
            sendMessageAfterDownload = settingsService.GetSetting("sendMessageAfterDownload") as bool? ?? true;
            fallBackToSoftwareEncoding = settingsService.GetSetting("fallBackToSoftwareEncoding") as bool? ?? true;
            autoRetryDownload = settingsService.GetSetting("autoRetryDownload") as bool? ?? true;
            countOfDownloadRetries = settingsService.GetSetting("countOfDownloadRetries") as int? ?? 3;
            ValidateSettingOrSetToDefault(ref countOfDownloadRetries, val => val is >= 0 and <= 5, 3);

            customAccentColorHex = settingsService.GetSetting("customAccentColorHex") as string;
            useCustomAccentColor = settingsService.GetSetting("useCustomAccentColor") as bool? ?? false;
            showDonationBanner = settingsService.GetSetting("showDonationBanner") as bool? ?? true;
            selectedTheme = ParseEnum<ThemeType>(settingsService.GetSetting("selectedTheme"));
            appFlowDirection = ParseEnum<AppFlowDirection>(settingsService.GetSetting("appFlowDirection"));


            useLogging=settingsService.GetSetting("useLogging") as bool? ?? false;
            logPath = settingsService.GetSetting("logPath") as string?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OnionMedia", "Logs"); ;

            var downloadsAudioFormat = settingsService.GetSetting("downloadsAudioFormat");
            if (downloadsAudioFormat == null)
                this.downloadsAudioFormat = AudioConversionFormat.Mp3;
            else
                this.downloadsAudioFormat = ParseEnum<AudioConversionFormat>(downloadsAudioFormat);

            var videoAddMode = settingsService.GetSetting("videoAddMode");
            if (videoAddMode == null)
                this.videoAddMode = VideoAddMode.AskForVideoAddMode;
            else
                this.videoAddMode = ParseEnum<VideoAddMode>(videoAddMode);

            var startPageType = settingsService.GetSetting("startPageType");
            if (startPageType == null)
                this.startPageType = StartPageType.LastOpened;
            else
                this.startPageType = ParseEnum<StartPageType>(startPageType);
        }

        private readonly ISettingsService settingsService = IoC.Default.GetService<ISettingsService>();

        public static AppSettings Instance { get; } = new AppSettings();
        public static VideoAddMode[] VideoAddModes { get; } = Enum.GetValues<VideoAddMode>().ToArray();
        public static StartPageType[] StartPageTypes { get; } = Enum.GetValues<StartPageType>().ToArray();
        public static ThemeType[] ThemeTypes { get; } = Enum.GetValues<ThemeType>().ToArray();
        public static AppFlowDirection[] FlowDirections { get; } = Enum.GetValues<AppFlowDirection>().ToArray();

        //Settings

        public bool UseLogging
        {
            get => useLogging.HasValue ? (bool)useLogging:false;
            set => SetSetting(ref useLogging, value, "useLogging");
        }
        private bool? useLogging;
        public string LogPath
        {
            get => logPath;
            set => SetSetting(ref logPath, value, "logPath");
        }
        public string logPath;
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

        public bool AutoRetryDownload
        {
            get => autoRetryDownload.HasValue && (bool)autoRetryDownload;
            set => SetSetting(ref autoRetryDownload, value, "autoRetryDownload");
        }
        private bool? autoRetryDownload;

        public int CountOfDownloadRetries
        {
            get => countOfDownloadRetries.HasValue ? (int)countOfDownloadRetries : 5;
            set => SetSetting(ref countOfDownloadRetries, value, "countOfDownloadRetries");
        }
        private int? countOfDownloadRetries;

        public HardwareEncoder HardwareEncoder
        {
            get => (HardwareEncoder)hardwareEncoder;
            set
            {
                if (SetProperty(ref hardwareEncoder, value))
                    settingsService.SetSetting("hardwareEncoder", value.ToString());
            }
        }
        private HardwareEncoder? hardwareEncoder;

        public AudioConversionFormat DownloadsAudioFormat
        {
            get => downloadsAudioFormat;
            set
            {
                if (SetProperty(ref downloadsAudioFormat, value))
                    settingsService.SetSetting("downloadsAudioFormat", value.ToString());
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

        public bool UseCustomAccentColor
        {
            get => useCustomAccentColor;
            set => SetSetting(ref useCustomAccentColor, value, "useCustomAccentColor");
        }
        private bool useCustomAccentColor;
        
        public string? CustomAccentColorHex
        {
            get => customAccentColorHex;
            set => SetSetting(ref customAccentColorHex, value, "customAccentColorHex");
        }
        private string? customAccentColorHex;
        
        public ThemeType SelectedTheme
        {
            get => selectedTheme;
            set
            {
                if (SetProperty(ref selectedTheme, value))
                    settingsService.SetSetting("selectedTheme", value.ToString());
            }
        }

        private ThemeType selectedTheme;
        
        public AppFlowDirection AppFlowDirection
        {
            get => appFlowDirection;
            set
            {
                if (SetProperty(ref appFlowDirection, value))
                    settingsService.SetSetting("appFlowDirection", value.ToString());
            }
        }

        private AppFlowDirection appFlowDirection;

        //Last opened page
        public bool DownloaderPageIsOpen
        {
            get => settingsService.GetSetting("downloaderPageIsOpen") as bool? ?? false;
            set => settingsService.SetSetting("downloaderPageIsOpen", value);
        }

        public bool ShowDonationBanner
        {
	        get => showDonationBanner.HasValue && (bool)showDonationBanner;
	        set => SetSetting(ref showDonationBanner, value, "showDonationBanner");
        }
        private bool? showDonationBanner;

		public StartPageType StartPageType
        {
            get => startPageType;
            set
            {
                if (SetProperty(ref startPageType, value))
                    settingsService.SetSetting("startPageType", value.ToString());
            }
        }
        private StartPageType startPageType;

        public VideoAddMode VideoAddMode
        {
            get => videoAddMode;
            set
            {
                if (SetProperty(ref videoAddMode, value))
                    settingsService.SetSetting("videoAddMode", value.ToString());
            }
        }
        private VideoAddMode videoAddMode;


        private void SetSetting<T>(ref T field, T value, string settingName, bool forceOnPropertyChanged = false, [CallerMemberName] string propName = null)
        {
            if (SetProperty(ref field, value, propName))
                settingsService.SetSetting(settingName, value);
            else if (forceOnPropertyChanged)
                OnPropertyChanged(propName);
        }

        private static bool ValidateSettingOrSetToDefault<T>(ref T fieldValue, Func<T, bool> validation, T defaultValue)
        {
            bool result = validation(fieldValue);
            if (!result)
                fieldValue = defaultValue;
            
            return result;
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
        DownloadedAudiofiles,
        LogPath
    }

    public enum StartPageType
    {
        LastOpened,
        ConverterPage,
        DownloaderPage
    }

    public enum ThemeType
    {
        Default,
        Light,
        Dark
    }

    public enum AppFlowDirection
    {
        LeftToRight,
        RightToLeft
    }
}

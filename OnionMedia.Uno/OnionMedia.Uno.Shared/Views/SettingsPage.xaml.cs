/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using CommunityToolkit.Mvvm.DependencyInjection;

using Microsoft.UI.Xaml.Controls;

using OnionMedia.Core.ViewModels;
using System;
using Windows.Globalization.NumberFormatting;
using OnionMedia.Core;
using OnionMedia.Core.Enums;
using OnionMedia.Core.Extensions;
using OnionMedia.Core.Models;
using YoutubeDLSharp.Options;

namespace OnionMedia.Uno.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; }
        private DecimalFormatter RoundingFormatter { get; }

        #region StaticReferences
        private HardwareEncoder[] HardwareEncoders => GlobalResources.HardwareEncoders;
        private AudioConversionFormat[] AudioConversionFormats => GlobalResources.AudioConversionFormats;
        string INVALIDFILENAMECHARACTERSREGEX => GlobalResources.INVALIDFILENAMECHARACTERSREGEX;
        int SystemThreadCount => GlobalResources.SystemThreadCount;
        AppSettings AppSettings => AppSettings.Instance;
        VideoAddMode[] VideoAddModes => AppSettings.VideoAddModes;

        string AmountOfSimultaneousOperationsHeader => "amountOfSimultaneousOperationsHeader".GetLocalized("SettingsPage");
        private string HardwareEncoderHeader => "hardwareEncoderHeader".GetLocalized("SettingsPage");
        #endregion

        public SettingsPage()
        {
            ViewModel = Ioc.Default.GetService<SettingsViewModel>();
            DecimalFormatter formatter = new()
            {
                IntegerDigits = 1,
                FractionDigits = 0,
                NumberRounder = new IncrementNumberRounder() { Increment = 1, RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp }
            };
            RoundingFormatter = formatter;

            InitializeComponent();
        }
    }
}

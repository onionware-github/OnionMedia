/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using OnionMedia.Core;
using OnionMedia.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OnionMedia.Views.Dialogs
{
    [INotifyPropertyChanged]
    public sealed partial class ConversionPresetDialog : ContentDialog
    {
        public ConversionPresetDialog(IEnumerable<string> forbiddenNames = null)
        {
            InitializeComponent();
            ConversionPreset = new();
            title = "titleNewPreset".GetLocalized(RESOURCEPATH);
            primaryButtonText = "primarybuttoncreate".GetLocalized(RESOURCEPATH);
            this.forbiddenNames = forbiddenNames;
        }

        public ConversionPresetDialog(ConversionPreset conversionPreset, IEnumerable<string> forbiddenNames = null)
        {
            if (conversionPreset == null)
                throw new ArgumentNullException(nameof(conversionPreset));
            InitializeComponent();
            ConversionPreset = conversionPreset.Clone();
            title = "titleEditPreset".GetLocalized(RESOURCEPATH);
            primaryButtonText = "primarybuttonapply".GetLocalized(RESOURCEPATH);
            if (forbiddenNames != null)
                this.forbiddenNames = forbiddenNames.Where(n => n != conversionPreset.Name);

            if (GlobalResources.FFmpegCodecs.Videocodecs.Any(c => c.Name.Equals(conversionPreset.VideoCodec.Name)))
                ConversionPreset.VideoCodec = GlobalResources.FFmpegCodecs.Videocodecs.First(c => c.Name.Equals(conversionPreset.VideoCodec.Name));

            if (GlobalResources.FFmpegCodecs.Audiocodecs.Any(c => c.Name.Equals(conversionPreset.AudioCodec.Name)))
                ConversionPreset.AudioCodec = GlobalResources.FFmpegCodecs.Audiocodecs.First(c => c.Name.Equals(conversionPreset.AudioCodec.Name));

            if (ConversionPreset.VideoCodec.Encoders.Any(e => e.Equals(conversionPreset.VideoEncoder)))
                ConversionPreset.VideoEncoder = ConversionPreset.VideoCodec.Encoders.First(e => e.Equals(conversionPreset.VideoEncoder));

            if (ConversionPreset.AudioCodec.Encoders.Any(e => e.Equals(conversionPreset.AudioEncoder)))
                ConversionPreset.AudioEncoder = ConversionPreset.AudioCodec.Encoders.First(e => e.Equals(conversionPreset.AudioEncoder));
        }

        private string PresetName
        {
            get => ConversionPreset.Name;
            set
            {
                if (ConversionPreset.Name == value) return;
                ConversionPreset.Name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NameAlreadyInUse));
                OnPropertyChanged(nameof(NameIsEmpty));
                OnPropertyChanged(nameof(ValidName));
            }
        }

        private bool NameAlreadyInUse => forbiddenNames != null && forbiddenNames.Contains(PresetName);
        private bool NameIsEmpty => string.IsNullOrWhiteSpace(PresetName);
        private bool ValidName => !NameIsEmpty && !NameAlreadyInUse;

        public ConversionPreset ConversionPreset { get; }

        readonly string title;
        readonly string primaryButtonText;
        readonly IEnumerable<string> forbiddenNames;
        const string RESOURCEPATH = "ConversionPresetDialog";
    }
}

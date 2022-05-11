/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>
 */

using System;
using System.Threading.Tasks;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;

using OnionMedia.Contracts.Services;
using OnionMedia.Core.Models;
using OnionMedia.Helpers;

using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace OnionMedia.ViewModels
{
    [ObservableObject]
    public sealed partial class SettingsViewModel
    {
        public SettingsViewModel(IThemeSelectorService themeSelectorService)
        {
            _themeSelectorService = themeSelectorService;
            _elementTheme = _themeSelectorService.Theme;
            VersionDescription = GetVersionDescription();
            ChangePathCommand = new(pt => ChangePathAsync(pt));
        }

        public AsyncRelayCommand<PathType> ChangePathCommand { get; }

        private readonly IThemeSelectorService _themeSelectorService;
        private ElementTheme _elementTheme;

        public ElementTheme ElementTheme
        {
            get { return _elementTheme; }

            set { SetProperty(ref _elementTheme, value); }
        }

        private string _versionDescription;

        public string VersionDescription
        {
            get { return _versionDescription; }

            set { SetProperty(ref _versionDescription, value); }
        }

        private ICommand _switchThemeCommand;

        public ICommand SwitchThemeCommand
        {
            get
            {
                if (_switchThemeCommand == null)
                {
                    _switchThemeCommand = new RelayCommand<ElementTheme>(
                        async (param) =>
                        {
                            if (ElementTheme != param)
                            {
                                ElementTheme = param;
                                await _themeSelectorService.SetThemeAsync(param);
                            }
                        });
                }

                return _switchThemeCommand;
            }
        }

        private static async Task ChangePathAsync(PathType pathType)
        {
            FolderPicker picker = new();
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
            var result = await picker.PickSingleFolderAsync();
            if (result == null) return;
            //TODO: Check write-access to the folder
            switch (pathType)
            {
                case PathType.ConvertedVideofiles:
                    AppSettings.Instance.ConvertedVideoSavePath = result.Path;
                    break;

                case PathType.ConvertedAudiofiles:
                    AppSettings.Instance.ConvertedAudioSavePath = result.Path;
                    break;

                case PathType.DownloadedVideofiles:
                    AppSettings.Instance.DownloadsVideoSavePath = result.Path;
                    break;

                case PathType.DownloadedAudiofiles:
                    AppSettings.Instance.DownloadsAudioSavePath = result.Path;
                    break;
            }
        }

        [ObservableProperty]
        private bool invalidFilename;

        private string GetVersionDescription()
        {
            var appName = "OnionMedia";
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

            return $"{appName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}

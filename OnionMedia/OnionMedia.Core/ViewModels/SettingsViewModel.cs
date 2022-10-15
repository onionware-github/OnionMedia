/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using OnionMedia.Core.Models;
using OnionMedia.Core.Services;
using OnionMedia.Core.Extensions;

namespace OnionMedia.Core.ViewModels
{
    [ObservableObject]
    public sealed partial class SettingsViewModel
    {
        public SettingsViewModel(IUrlService urlService, IDialogService dialogService, IThirdPartyLicenseDialog thirdPartyLicenseDialog, IPathProvider pathProvider, IVersionService versionService)
        {
            this.dialogService = dialogService;
            this.urlService = urlService;
            this.thirdPartyLicenseDialog = thirdPartyLicenseDialog;
            this.pathProvider = pathProvider;
            this.versionService = versionService;
            VersionDescription = GetVersionDescription();
        }

        private readonly IDialogService dialogService;
        private readonly IUrlService urlService;
        private readonly IThirdPartyLicenseDialog thirdPartyLicenseDialog;
        private readonly IPathProvider pathProvider;
        private readonly IVersionService versionService;

        private string _versionDescription;

        public string VersionDescription
        {
            get { return _versionDescription; }

            set { SetProperty(ref _versionDescription, value); }
        }

        [ICommand]
        private async Task ChangePathAsync(PathType pathType)
        {
            var path = await dialogService.ShowFolderPickerDialogAsync(DirectoryLocation.Videos);
            if (path == null) return;

            //TODO: Check write-access to the folder
            switch (pathType)
            {
                case PathType.ConvertedVideofiles:
                    AppSettings.Instance.ConvertedVideoSavePath = path;
                    break;

                case PathType.ConvertedAudiofiles:
                    AppSettings.Instance.ConvertedAudioSavePath = path;
                    break;

                case PathType.DownloadedVideofiles:
                    AppSettings.Instance.DownloadsVideoSavePath = path;
                    break;

                case PathType.DownloadedAudiofiles:
                    AppSettings.Instance.DownloadsAudioSavePath = path;
                    break;
            }
        }

        [ICommand]
        private async Task ShowLicenseAsync()
        {
            string title = "licenseTitle".GetLocalized();
            string license = await File.ReadAllTextAsync(pathProvider.LicensesDir + "onionmedia.txt");
            await dialogService.ShowInfoDialogAsync(title, license, "OK");
        }

        [ICommand]
        private async Task ShowThirdPartyLicensesAsync() => await thirdPartyLicenseDialog.ShowThirdPartyLicensesDialogAsync();

        [ICommand]
        private async Task ShowThanksDialogAsync()
        {
            string title = "specialThanks".GetLocalized();
            string content = "specialThanksText".GetLocalized();
            await dialogService.ShowInfoDialogAsync(title, content, "OK");
        }

        [ICommand]
        private async Task OpenContactMailAsync() => await urlService.OpenUrlAsync("mailto:contact.onionware@gmail.com");

        [ObservableProperty]
        private bool invalidFilename;

        private string GetVersionDescription()
        {
            var appName = "OnionMedia";
            var version = versionService.GetCurrentVersion();

            return $"{appName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}

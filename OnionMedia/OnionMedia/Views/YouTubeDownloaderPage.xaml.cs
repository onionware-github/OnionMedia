/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OnionMedia.Core.ViewModels;

namespace OnionMedia.Views
{
    public sealed partial class YouTubeDownloaderPage : Page
    {
        public YouTubeDownloaderViewModel ViewModel { get; }

        public YouTubeDownloaderPage()
        {
            ViewModel = Ioc.Default.GetService<YouTubeDownloaderViewModel>();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            Loaded += OnLoaded;
            InitializeComponent();
        }

        //Workaround for a bug from WinUI 3 (freezing ProgressRing)
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            btnProgressRing.IsActive = false;
            btnProgressRing.IsActive = true;
            rotateInSearchIconTrigger.Value = false;
            videolink.Focus(FocusState.Programmatic);
        }

        private void RemoveAll_Clicked(object sender, RoutedEventArgs e)
        {
            removeBtnFlyout.Hide();
        }
    }
}

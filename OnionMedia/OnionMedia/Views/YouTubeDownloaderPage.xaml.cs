/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using AngleSharp.Dom;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OnionMedia.Core.ViewModels;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.UI.Xaml.Navigation;
using OnionMedia.Core;
using OnionMedia.Core.Models;
using YoutubeExplode.Playlists;
using Visibility = Microsoft.UI.Xaml.Visibility;

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
            App.MainWindow.SizeChanged += (_, _) => OnSizeChanged();
            ViewModel.PropertyChanged += async (_, _) =>
            {
	            await Task.Delay(100);
	            OnSizeChanged();
            };
			searchResultsList.SizeChanged += (_, _) => OnSizeChanged();
			videoQueue.SizeChanged += (_, _) => OnSizeChanged();
		}

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
	        base.OnNavigatedTo(e);
            OnSizeChanged();
        }

        private void OnSizeChanged()
		{
			if (!AppSettings.Instance.ShowDonationBanner)
			{
				donationGrid.Visibility = Visibility.Collapsed;
				return;
			}

			// Check if ScrollViewer can scroll
			if (scrollViewer.ScrollableHeight == 0 && this.ActualHeight - scrollViewer.ViewportHeight > donationGrid.ActualHeight)
			{
				// If not, show donation banner
				donationGrid.Visibility = Visibility.Visible;
				return;
			}

			// Otherwise, hide banner and don't reserve screen space for it
			donationGrid.Visibility = Visibility.Collapsed;
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

        private void OpenDonationPage(object sender, RoutedEventArgs e)
        {
	        Process.Start(new ProcessStartInfo
			{
				FileName = GlobalResources.LocalDonationUrl,
				UseShellExecute = true
			});
		}
    }
}

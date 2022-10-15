/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using OnionMedia.Core.ViewModels;
using Windows.ApplicationModel.DataTransfer;

namespace OnionMedia.Views
{
    public sealed partial class MediaPage : Page
    {
        public MediaViewModel ViewModel { get; }

        public MediaPage()
        {
            ViewModel = Ioc.Default.GetService<MediaViewModel>();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            this.Loaded += OnLoaded;
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UIResources.XamlRoot = ((MediaPage)sender).XamlRoot;

            //Workaround for a bug from WinUI 3 (freezing ProgressRing)
            btnProgressRing.IsActive = false;
            btnProgressRing.IsActive = true;
        }

        private void Resolutionpreset_ItemClick(object sender, ItemClickEventArgs e) => resolutionFlyout.Hide();
        private void Frameratepreset_ItemClick(object sender, ItemClickEventArgs e) => framerateFlyout.Hide();
        private void SmallRemoveAll_Clicked(object sender, RoutedEventArgs e) => smallRemoveBtnFlyout.Hide();
        private void WideRemoveAll_Clicked(object sender, RoutedEventArgs e) => wideRemoveBtnFlyout.Hide();


        //Always set value to true on click.
        private void ToggleButton_Click(object sender, RoutedEventArgs e) => ((ToggleButton)sender).IsChecked = true;

        private async void mediaList_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel.AddFileCommand.IsRunning || ViewModel.StartConversionCommand.IsRunning || !e.DataView.Contains(StandardDataFormats.StorageItems))
                return;

            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count == 0) return;
            await ViewModel.AddFilesAsync(items.Select(i => i.Path));
        }

        private void mediaList_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = (ViewModel.AddFileCommand.IsRunning || ViewModel.StartConversionCommand.IsRunning) ? DataPackageOperation.None : DataPackageOperation.Link;
        }
    }
}

﻿/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>
 */

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using OnionMedia.ViewModels;

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
            GlobalResources.XamlRoot = ((MediaPage)sender).XamlRoot;

            //Workaround for a bug from WinUI 3 (freezing ProgressRing)
            btnProgressRing.IsActive = false;
            btnProgressRing.IsActive = true;
        }

        private void Resolutionpreset_ItemClick(object sender, ItemClickEventArgs e) => resolutionFlyout.Hide();
        private void Frameratepreset_ItemClick(object sender, ItemClickEventArgs e) => framerateFlyout.Hide();
        private void SmallRemoveAll_Clicked(object sender, RoutedEventArgs e) => smallRemoveBtnFlyout.Hide();
        private void WideRemoveAll_Clicked(object sender, RoutedEventArgs e) => wideRemoveBtnFlyout.Hide();


        //Always set value to true on click.
        private void ToggleButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => ((ToggleButton)sender).IsChecked = true;
    }
}

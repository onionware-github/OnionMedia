/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using OnionMedia.Contracts.Services;
using OnionMedia.Core.Models;
using OnionMedia.ViewModels;

using Windows.System;

namespace OnionMedia.Views
{
    // TODO WTS: Change the icons and titles for all NavigationViewItems in ShellPage.xaml.
    [INotifyPropertyChanged]
    public sealed partial class ShellPage : Page
    {
        private readonly KeyboardAccelerator _altLeftKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu);
        private readonly KeyboardAccelerator _backKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.GoBack);
        private readonly bool navigateToDownloadPage = AppSettings.Instance.DownloaderPageIsOpen;

        [ObservableProperty]
        private double pageWidth;
        [ObservableProperty]
        private double pageHeight;

        public ShellViewModel ViewModel { get; }

        public ShellPage(ShellViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
            ViewModel.NavigationService.Frame = shellFrame;
            ViewModel.NavigationViewService.Initialize(navigationView);
        }

        private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // Keyboard accelerators are added here to avoid showing 'Alt + left' tooltip on the page.
            // More info on tracking issue https://github.com/Microsoft/microsoft-ui-xaml/issues/8
            KeyboardAccelerators.Add(_altLeftKeyboardAccelerator);
            KeyboardAccelerators.Add(_backKeyboardAccelerator);
            ConfigureTitleBar();

            if (AppSettings.Instance.StartPageType is StartPageType.DownloaderPage || (AppSettings.Instance.StartPageType is StartPageType.LastOpened && navigateToDownloadPage))
                shellFrame.Navigate(typeof(YouTubeDownloaderPage), null, new SuppressNavigationTransitionInfo());
        }

        private void ConfigureTitleBar()
        {
            AppTitleBar.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            App.MainWindow.ExtendsContentIntoTitleBar = true;
            App.MainWindow.SetTitleBar(AppTitleBar);
        }

        private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
        {
            var keyboardAccelerator = new KeyboardAccelerator() { Key = key };
            if (modifiers.HasValue)
            {
                keyboardAccelerator.Modifiers = modifiers.Value;
            }

            keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;
            return keyboardAccelerator;
        }

        private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var navigationService = Ioc.Default.GetService<INavigationService>();
            var result = navigationService.GoBack();
            args.Handled = result;
        }

        private void Page_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            PageWidth = e.NewSize.Width;
            PageHeight = e.NewSize.Height;
        }
    }
}

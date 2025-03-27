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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using OnionMedia.Core.Enums;
using OnionMedia.Core.Services;
using OnionMedia.Core.ViewModels;
using Microsoft.UI;
using Microsoft.Extensions.Logging;
using OnionMedia.Services;
using System.Diagnostics;

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
		private MediaViewModel MediaViewModel { get; }
		private YouTubeDownloaderViewModel DownloaderViewModel { get; }
		private IPCPower PcPowerService { get; }

		private PCPowerOption desiredPowerOption;
		private bool executeOnError;
		public ILogger<ShellPage> logger;
        private readonly IVersionService versionService;

        public ShellPage(IVersionService versionService, ShellViewModel viewModel, MediaViewModel mediaViewModel, YouTubeDownloaderViewModel downloaderViewModel, IPCPower pcPowerService, ILogger<ShellPage> _logger)
		{
			this.versionService = versionService;
			logger = _logger;
			ViewModel = viewModel;
			InitializeComponent();
			ViewModel.NavigationService.Frame = shellFrame;
			ViewModel.NavigationViewService.Initialize(navigationView);
			PcPowerService = pcPowerService;
			MediaViewModel = mediaViewModel;
			DownloaderViewModel = downloaderViewModel;
			MediaViewModel.ConversionDone += ReactToProcessFinish;
			DownloaderViewModel.DownloadDone += ReactToProcessFinish;
			ActualThemeChanged += (_, _) => SetPowerIcon();
		}

		private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
            
            logger.LogDebug(GetVersionDescription());
            if (Debugger.IsAttached)
            {
                logger.LogDebug("App runs currently in debug-mode");
            }

            // Keyboard accelerators are added here to avoid showing 'Alt + left' tooltip on the page.
            // More info on tracking issue https://github.com/Microsoft/microsoft-ui-xaml/issues/8
            KeyboardAccelerators.Add(_altLeftKeyboardAccelerator);
			KeyboardAccelerators.Add(_backKeyboardAccelerator);
			ConfigureTitleBar();

			if (AppSettings.Instance.StartPageType is StartPageType.DownloaderPage || (AppSettings.Instance.StartPageType is StartPageType.LastOpened && navigateToDownloadPage))
				shellFrame.Navigate(typeof(YouTubeDownloaderPage), null, new SuppressNavigationTransitionInfo());
		}
        private string GetVersionDescription()
        {
            var appName = "OnionMedia";
            var version = versionService.GetCurrentVersion();

            return $"{appName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
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

		private void ShutdownBtn_OnClick(object sender, RoutedEventArgs e)
		{
			shutdownFlyout.PreferredPlacement = navigationView.DisplayMode is NavigationViewDisplayMode.Minimal ? TeachingTipPlacementMode.TopRight : TeachingTipPlacementMode.BottomLeft;
			ViewModel.ShutdownTipIsOpen = true;
			actionSelector.IsDropDownOpen = true;
			actionSelector.IsDropDownOpen = false;
		}

		private void ShutdownFlyout_OnCloseButtonClick(TeachingTip sender, object args)
		{
			ViewModel.SelectedPowerOption = desiredPowerOption;
			ViewModel.ExecuteOnError = executeOnError;
			ViewModel.ShutdownTipIsOpen = false;
		}
		
		private void ShutdownFlyout_OnActionButtonClick(TeachingTip sender, object args)
		{
			desiredPowerOption = ViewModel.SelectedPowerOption;
			executeOnError = ViewModel.ExecuteOnError;
			ViewModel.ShutdownTipIsOpen = false;
			SetPowerIcon();
		}

		private void NavigationView_OnDisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
		{
			ViewModel.ShowHeaderPowerButton = sender.DisplayMode is NavigationViewDisplayMode.Minimal;
		}

		private void ReactToProcessFinish(object sender, bool errors)
		{
			if  (desiredPowerOption is PCPowerOption.None ||
			    (errors && !executeOnError) ||
			    (sender is MediaViewModel && DownloaderViewModel.DownloadFileCommand.IsRunning) ||
			    (sender is YouTubeDownloaderViewModel && MediaViewModel.StartConversionCommand.IsRunning))
			{
				return;
			}

			switch (desiredPowerOption)
			{
				case PCPowerOption.Shutdown:
					PcPowerService.Shutdown();
					return;

				case PCPowerOption.Hibernate:
					PcPowerService.Hibernate();
					return;

				case PCPowerOption.Sleep:
					PcPowerService.Standby();
					return;
			}
		}

		private void SetPowerIcon()
		{
			switch (desiredPowerOption)
			{
				case PCPowerOption.Shutdown:
					shutdownIcon.Glyph = "\uE7E8";
					shutdownIcon.Foreground = new SolidColorBrush(Colors.OrangeRed);
					return;

				case PCPowerOption.Hibernate:
					shutdownIcon.Glyph = "\uE823";
					shutdownIcon.Foreground = new SolidColorBrush(Colors.DarkOrange);
					return;

				case PCPowerOption.Sleep:
					shutdownIcon.Glyph = "\uE708";
					shutdownIcon.Foreground = new SolidColorBrush(Colors.DarkOrange);
					return;

				case PCPowerOption.None:
					shutdownIcon.Glyph = "\uE7E8";
					shutdownIcon.Foreground = (Brush)Application.Current.Resources["ApplicationForegroundThemeBrush"];
					return;
			}
		}
	}
}

/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

using OnionMedia.Activation;
using OnionMedia.Contracts.Services;
using OnionMedia.Services;
using OnionMedia.ViewModels;
using OnionMedia.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.System;
using Windows.System.UserProfile;
using Windows.Storage;
using OnionMedia.Core.Models;
using FFMpegCore;
using OnionMedia.Core;
using OnionMedia.Core.Services;
using OnionMedia.Core.ViewModels;
using OnionMedia.Core.ViewModels;

// To learn more about WinUI3, see: https://docs.microsoft.com/windows/apps/winui/winui3/.
namespace OnionMedia
{
    public partial class App : Application
    {
        public static Window MainWindow { get; set; } = new Window() { Title = "OnionMedia" };

        public App()
        {
            InitializeComponent();
            UnhandledException += App_UnhandledException;
            var services = ConfigureServices();
            Ioc.Default.ConfigureServices(services);
            IoC.Default.InitializeServices(services);
            GlobalFFOptions.Configure(options => options.BinaryFolder = IoC.Default.GetService<IPathProvider>().ExternalBinariesDir);
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // TODO WTS: Please log and handle the exception as appropriate to your scenario
            // For more info see https://docs.microsoft.com/windows/winui/api/microsoft.ui.xaml.unhandledexceptioneventargs
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);
            var activationService = Ioc.Default.GetService<IActivationService>();

            try
            {
                // Listen to notification activation
                ToastNotificationManagerCompat.OnActivated += async toastArgs =>
                {
                    // Obtain the arguments from the notification
                    ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

                    // Obtain any user input (text boxes, menu selections) from the notification
                    ValueSet userInput = toastArgs.UserInput;

                    Debug.WriteLine("Toast activated!");

                    if (args.Contains("action", "play"))
                    {
                        string filepath = args.Get("filepath");
                        Debug.WriteLine(filepath);

                        if (File.Exists(filepath))
                            await Launcher.LaunchUriAsync(new Uri(filepath));
                    }
                    else if (args.Contains("action", "open path"))
                    {
                        string folderpath = Path.GetDirectoryName(args.Get("folderpath"));

                        //Select the new files
                        FolderLauncherOptions folderLauncherOptions = new();
                        if (args.TryGetValue("filenames", out string filenames))
                            foreach (var file in filenames.Split('\n'))
                                folderLauncherOptions.ItemsToSelect.Add(await StorageFile.GetFileFromPathAsync(Path.Combine(folderpath, file)));

                        if (Directory.Exists(folderpath))
                            await Launcher.LaunchFolderPathAsync(folderpath, folderLauncherOptions);
                    }

                    if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
                        Environment.Exit(0);
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                var ffmpegStartup = IoC.Default.GetService<IFFmpegStartup>();
                await ffmpegStartup.InitializeFormatsAndCodecsAsync();
                await activationService.ActivateAsync(args);
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            //Register your services, viewmodels and pages here
            var services = new ServiceCollection();

            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers

            // Services
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IDownloaderDialogService, DownloaderDialogService>();
            services.AddSingleton<IThirdPartyLicenseDialog, ThirdPartyLicenseDialog>();
            services.AddSingleton<IConversionPresetDialog, ConversionPresetDialog>();
            services.AddSingleton<IFiletagEditorDialog, FiletagEditorDialog>();
            services.AddSingleton<ICustomPresetSelectorDialog, CustomPresetSelectorDialog>();
            services.AddSingleton<IDispatcherService, DispatcherService>();
            services.AddSingleton<INetworkStatusService, NetworkStatusService>();
            services.AddSingleton<IUrlService, UrlService>();
            services.AddSingleton<IToastNotificationService, ToastNotificationService>();
            services.AddSingleton<IStringResourceService, StringResourceService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IPathProvider, PathProvider>();
            services.AddSingleton<IVersionService, VersionService>();
            services.AddSingleton<IWindowClosingService, WindowClosingService>();
            services.AddSingleton<IFFmpegStartup, FFmpegStartup>();

            // Views and ViewModels
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();
            services.AddTransient<MediaViewModel>();
            services.AddTransient<MediaPage>();
            services.AddTransient<YouTubeDownloaderViewModel>();
            services.AddTransient<YouTubeDownloaderPage>();
            services.AddTransient<PlaylistsViewModel>();
            services.AddTransient<PlaylistsPage>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            return services.BuildServiceProvider();
        }
    }
}

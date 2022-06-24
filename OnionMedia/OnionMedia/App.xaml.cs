/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>
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
using OnionMedia.Core.Services;

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
            Ioc.Default.ConfigureServices(ConfigureServices());
            GlobalFFOptions.Configure(options => options.BinaryFolder = GlobalResources.Installpath + @"\ExternalBinaries\ffmpeg+yt-dlp\binaries");
            GlobalResources.DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
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

            byte[] hash;
            FFmpegCodecConfig codecs;
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(GlobalResources.FFmpegPath))
                    hash = md5.ComputeHash(stream);
            }

            string serializedCodecsPath = ApplicationData.Current.LocalCacheFolder.Path + @"\codecs.json";
            try
            {
                try
                {
                    //Try to read from codecs.json file
                    codecs = JsonSerializer.Deserialize<FFmpegCodecConfig>(File.ReadAllText(Path.Combine(GlobalResources.Installpath, @"Data\codecs.json")));
                    GlobalResources.FFmpegCodecs = codecs.FFmpegHash.SequenceEqual(hash) ? codecs : throw new Exception();
                }
                catch
                {
                    //When something goes wrong while deserializing the file, look at the LocalCache Directory
                    codecs = JsonSerializer.Deserialize<FFmpegCodecConfig>(await File.ReadAllTextAsync(serializedCodecsPath));
                    GlobalResources.FFmpegCodecs = codecs.FFmpegHash.SequenceEqual(hash) ? codecs : throw new Exception();
                }
            }
            catch
            {
                //When no (valid) file was found, generate a new one in the LocalCache Directory
                GlobalResources.FFmpegCodecs = new FFmpegCodecConfig(FFmpegCodec.GetEncodableVideoCodecs(), FFmpegCodec.GetEncodableAudioCodecs(), FFMpeg.GetContainerFormats().Where(f => f.MuxingSupported).Select(f => new FFmpegContainerFormat(f.Name, f.Description)), hash);
                File.WriteAllText(serializedCodecsPath, JsonSerializer.Serialize(GlobalResources.FFmpegCodecs));
            }
            finally
            {
                await activationService.ActivateAsync(args);
            }
        }

        private IServiceProvider ConfigureServices()
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
            services.AddSingleton<ICustomDialogService, CustomDialogService>();

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

using Jab;
using Microsoft.UI.Xaml;
using OnionMedia.Activation;
using OnionMedia.Contracts.Services;
using OnionMedia.Core.Models;
using OnionMedia.Core.Services;
using OnionMedia.Core.Services.Implementations;
using OnionMedia.Core.ViewModels;
using OnionMedia.Services;
using OnionMedia.ViewModels;
using OnionMedia.Views;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using Windows.Storage;
using System.Threading;

namespace OnionMedia;

//Services/Activation Handler
[ServiceProvider]

//Logging
[Singleton(typeof(ILoggerFactory), Factory = nameof(CreateLoggerFactory))]
[Singleton(typeof(Serilog.ILogger), Factory = nameof(CreateSerilogLogger))]
[Transient(typeof(ILogger<>), Factory = nameof(CreateLogger))]




[Singleton(typeof(IThemeSelectorService), typeof(ThemeSelectorService))]
[Singleton(typeof(IPageService), typeof(PageService))]
[Singleton(typeof(INavigationService), typeof(NavigationService))]
[Transient(typeof(INavigationViewService), typeof(NavigationViewService))]
[Transient(typeof(ActivationHandler<LaunchActivatedEventArgs>), typeof(DefaultActivationHandler))]
[Singleton(typeof(IActivationService), typeof(ActivationService))]

//Core Services
[Singleton(typeof(IDataCollectionProvider<LibraryInfo>), typeof(LibraryInfoProvider))]
[Singleton(typeof(IDialogService), typeof(DialogService))]
[Singleton(typeof(IDownloaderDialogService), typeof(DownloaderDialogService))]
[Singleton(typeof(IThirdPartyLicenseDialog), typeof(ThirdPartyLicenseDialog))]
[Singleton(typeof(IConversionPresetDialog), typeof(ConversionPresetDialog))]
[Singleton(typeof(IFiletagEditorDialog), typeof(FiletagEditorDialog))]
[Singleton(typeof(ICustomPresetSelectorDialog), typeof(CustomPresetSelectorDialog))]
[Singleton(typeof(IDispatcherService), typeof(DispatcherService))]
[Singleton(typeof(INetworkStatusService), typeof(NetworkStatusService))]
[Singleton(typeof(IUrlService), typeof(UrlService))]
[Singleton(typeof(ITaskbarProgressService), typeof(TaskbarProgressService))]
[Singleton(typeof(IToastNotificationService), typeof(ToastNotificationService))]
[Singleton(typeof(IStringResourceService), typeof(StringResourceService))]
[Singleton(typeof(ISettingsService), typeof(SettingsService))]
[Singleton(typeof(IPathProvider), typeof(PathProvider))]
[Singleton(typeof(IVersionService), typeof(VersionService))]
[Singleton(typeof(IWindowClosingService), typeof(WindowClosingService))]
[Singleton(typeof(IPCPower), typeof(WindowsPowerService))]
[Singleton(typeof(IFFmpegStartup), typeof(FFmpegStartup))]

//Views and ViewModels
[Transient(typeof(ShellViewModel))]
[Transient(typeof(ShellPage))]
[Singleton(typeof(MediaViewModel))]
[Transient(typeof(MediaPage))]
[Singleton(typeof(YouTubeDownloaderViewModel))]
[Transient(typeof(YouTubeDownloaderPage))]
[Transient(typeof(SettingsViewModel))]
[Transient(typeof(SettingsPage))]
[Transient(typeof(PlaylistsViewModel))]
[Transient(typeof(PlaylistsPage))]
sealed partial class ServiceProvider
{
    private static string logfile = Path.Combine(AppSettings.Instance.LogPath, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

    private static Serilog.ILogger CreateSerilogLogger()
    {
        if (!Directory.Exists("logs"))
        {
            Directory.CreateDirectory("logs");
        }
        CheckAge();
        if (AppSettings.Instance.UseLogging)
        {
            return new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logfile)
            .CreateLogger();
        }
        else
        {
            return new LoggerConfiguration()
           .MinimumLevel.Fatal() // There are no fatal log-messages in code so nothing will be loggt.
           .CreateLogger();
        }
    }
    private static ILogger<T> CreateLogger<T>(IServiceProvider serviceProvider)
    {
        var factory = serviceProvider.GetService<ILoggerFactory>();
        return factory.CreateLogger<T>();
    }

    private static ILoggerFactory CreateLoggerFactory()
    {
        var logger = CreateSerilogLogger();
        return LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(logger);
        });
    }
    private static void CheckAge()
    {
        var files = Directory.EnumerateFiles(AppSettings.Instance.LogPath, "*");
        foreach (var file in files) 
        {
            DateTime lastModified = File.GetLastWriteTime(file);
            if ((DateTime.Now - lastModified).TotalDays > 7)
            {
                File.Delete(file);
            }
        }
    }

}

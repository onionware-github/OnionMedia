using Microsoft.UI.Xaml;
using Jab;
using OnionMedia.Activation;
using OnionMedia.Contracts.Services;
using OnionMedia.Core.Models;
using OnionMedia.Core.Services;
using OnionMedia.Core.ViewModels;
using OnionMedia.Services;
using OnionMedia.ViewModels;
using OnionMedia.Views;

namespace OnionMedia;

//Services/Activation Handler
[ServiceProvider]
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
sealed partial class ServiceProvider { }
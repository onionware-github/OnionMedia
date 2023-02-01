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
using WinRT;
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
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Composition.SystemBackdrops;
using System.Runtime.InteropServices;

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
            var services = new ServiceProvider();
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
                CenterMainWindow();
                SubClassing();
                await activationService.ActivateAsync(args);

                //Enable Mica
                TrySetSystemBackdrop();
            }
        }

        static void CenterMainWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            if (appWindow is null) return;

            DisplayArea displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
            if (displayArea is null) return;

            var CenteredPosition = appWindow.Position;
            CenteredPosition.X = ((displayArea.WorkArea.Width - appWindow.Size.Width) / 2);
            CenteredPosition.Y = ((displayArea.WorkArea.Height - appWindow.Size.Height) / 2);
            appWindow.Move(CenteredPosition);
        }


        WindowsSystemDispatcherQueueHelper m_wsdqHelper; // See below for implementation.
        MicaController m_backdropController;
        SystemBackdropConfiguration m_configurationSource;

        bool TrySetSystemBackdrop()
        {
            if (!MicaController.IsSupported()) return false; // Mica is not supported on this system

            m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            // Create the policy object.
            m_configurationSource = new SystemBackdropConfiguration();
            MainWindow.Activated += Window_Activated;
            MainWindow.Closed += Window_Closed;
            ((FrameworkElement)MainWindow.Content) .ActualThemeChanged+= Window_ThemeChanged;

            // Initial configuration state.
            m_configurationSource.IsInputActive = true;
            SetConfigurationSourceTheme();

            m_backdropController = new MicaController();

            // Enable the system backdrop.
            // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
            m_backdropController.AddSystemBackdropTarget(MainWindow.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            m_backdropController.SetSystemBackdropConfiguration(m_configurationSource);
            return true; // succeeded
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed
            // so it doesn't try to use this closed window.
            if (m_backdropController != null)
            {
                m_backdropController.Dispose();
                m_backdropController = null;
            }
            MainWindow.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)MainWindow.Content).ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
            }
        }

        //Native resizing stuff
        private delegate IntPtr WinProc(IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam);
        private WinProc newWndProc = null;
        private IntPtr oldWndProc = IntPtr.Zero;
        [DllImport("user32")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc);
        [DllImport("user32.dll")]
        static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam);

        private void SubClassing()
        {
            //Get the Window's HWND
            var hwnd = MainWindow.As<IWindowNative>().WindowHandle;

            newWndProc = new WinProc(NewWindowProc);
            oldWndProc = SetWindowLongPtr(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_WNDPROC, newWndProc);
        }

        int MinWidth = 330;
        int MinHeight = 520;

        [StructLayout(LayoutKind.Sequential)]
        struct MINMAXINFO
        {
            public PInvoke.POINT ptReserved;
            public PInvoke.POINT ptMaxSize;
            public PInvoke.POINT ptMaxPosition;
            public PInvoke.POINT ptMinTrackSize;
            public PInvoke.POINT ptMaxTrackSize;
        }

        private IntPtr NewWindowProc(IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam)
        {
            switch (Msg)
            {
                case PInvoke.User32.WindowMessage.WM_GETMINMAXINFO:
                    var dpi = PInvoke.User32.GetDpiForWindow(hWnd);
                    float scalingFactor = (float)dpi / 96;

                    MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    minMaxInfo.ptMinTrackSize.x = (int)(MinWidth * scalingFactor);
                    minMaxInfo.ptMinTrackSize.y = (int)(MinHeight * scalingFactor);
                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;

            }
            return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
        }


        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }
    }
}
class WindowsSystemDispatcherQueueHelper
{
    [StructLayout(LayoutKind.Sequential)]
    struct DispatcherQueueOptions
    {
        internal int dwSize;
        internal int threadType;
        internal int apartmentType;
    }

    [DllImport("CoreMessaging.dll")]
    private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

    object m_dispatcherQueueController = null;
    public void EnsureWindowsSystemDispatcherQueueController()
    {
        if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
        {
            // one already exists, so we'll just use it.
            return;
        }

        if (m_dispatcherQueueController == null)
        {
            DispatcherQueueOptions options;
            options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
            options.threadType = 2;    // DQTYPE_THREAD_CURRENT
            options.apartmentType = 2; // DQTAT_COM_STA

            CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
        }
    }
}
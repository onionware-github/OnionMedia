using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OnionMedia.Core.Enums;
using OnionMedia.Core.Services;

namespace OnionMedia.Services;

sealed class TaskbarProgressService : ITaskbarProgressService
{
    private Type currentVmType;

    public ProgressBarState CurrentState { get; private set; }

    public void UpdateProgress(Type senderType, float progress)
    {
        if (senderType != currentVmType) return;
        if (progress < 0) progress = 0;
        if (progress > 100) progress = 100;

        var val = (ulong)Math.Round(progress, 0);
        SetTaskbarState(TBPFLAG.TBPF_NORMAL);
        SetTaskbarProgress(val);
        CurrentState = ProgressBarState.Loading;
    }

    public void UpdateState(Type senderType, ProgressBarState state)
    {
        if (senderType != currentVmType) return;

        if (state is ProgressBarState.Error)
            SetTaskbarProgress(100);

        if (state is ProgressBarState.None)
            SetTaskbarProgress(0);

        SetTaskbarState(ConvertToTBPFLAG(state));
        CurrentState = state;
    }

    public void SetType(Type type)
    {
        if (currentVmType == type) return;
        currentVmType = type;
    }


    private static void SetTaskbarProgress(ulong progress)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        taskbar.SetProgressValue(hwnd, progress, 100);
    }

    private static void SetTaskbarState(TBPFLAG state)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        taskbar.SetProgressState(hwnd, state);
    }

    private static ITaskbarList3 taskbar = (ITaskbarList3)new TaskbarInstance();

    private enum TBPFLAG
    {
        TBPF_NOPROGRESS = 0,
        TBPF_INDETERMINATE = 0x1,
        TBPF_NORMAL = 0x2,
        TBPF_ERROR = 0x4,
        TBPF_PAUSED = 0x8
    }

    private static TBPFLAG ConvertToTBPFLAG(ProgressBarState state) => state switch
    {
        ProgressBarState.Error => TBPFLAG.TBPF_ERROR,
        ProgressBarState.Indeterminate => TBPFLAG.TBPF_INDETERMINATE,
        ProgressBarState.Loading => TBPFLAG.TBPF_NORMAL,
        ProgressBarState.None => TBPFLAG.TBPF_NOPROGRESS,
        ProgressBarState.Paused => TBPFLAG.TBPF_PAUSED,
        _ => TBPFLAG.TBPF_NOPROGRESS
    };

    [ComImport()]
    [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITaskbarList3
    {
        // ITaskbarList
        [PreserveSig]
        void HrInit();
        [PreserveSig]
        void AddTab(IntPtr hwnd);
        [PreserveSig]
        void DeleteTab(IntPtr hwnd);
        [PreserveSig]
        void ActivateTab(IntPtr hwnd);
        [PreserveSig]
        void SetActiveAlt(IntPtr hwnd);

        // ITaskbarList2
        [PreserveSig]
        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

        // ITaskbarList3
        [PreserveSig]
        void SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal);
        [PreserveSig]
        void SetProgressState(IntPtr hwnd, TBPFLAG state);
    }

    [ComImport()]
    [Guid("56fdf344-fd6d-11d0-958a-006097c9a090")]
    [ClassInterface(ClassInterfaceType.None)]
    private class TaskbarInstance {}
}
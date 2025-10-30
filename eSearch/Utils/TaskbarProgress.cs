using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace eSearch.Utils
{
    public static class TaskbarProgress
    {
        public enum TaskbarStates
        {
            NoProgress = 0,
            Indeterminate = 0x1,
            Normal = 0x2,
            Error = 0x4,
            Paused = 0x8
        }

        [ComImport]
        [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList3
        {
            // ITaskbarList
            [PreserveSig] void HrInit();
            [PreserveSig] void AddTab(IntPtr hwnd);
            [PreserveSig] void DeleteTab(IntPtr hwnd);
            [PreserveSig] void ActivateTab(IntPtr hwnd);
            [PreserveSig] void SetActiveAlt(IntPtr hwnd);

            // ITaskbarList2
            [PreserveSig] void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

            // ITaskbarList3 (relevant methods)
            [PreserveSig] void SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal);
            [PreserveSig] void SetProgressState(IntPtr hwnd, TaskbarStates state);
        }

        [ComImport]
        [Guid("56fdf344-fd6d-11d0-958a-006097c9a090")]
        [ClassInterface(ClassInterfaceType.None)]
        private class TaskbarInstance { }

        private static readonly ITaskbarList3 _taskbarInstance = (ITaskbarList3)new TaskbarInstance();
        private static readonly bool _taskbarSupported = Environment.OSVersion.Version >= new Version(6, 1);

        public static void SetState(Window window, TaskbarStates state)
        {
            if (_taskbarSupported && window != null)
            {
                IntPtr hwnd = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (hwnd != IntPtr.Zero)
                {
                    _taskbarInstance.SetProgressState(hwnd, state);
                }
            }
        }

        public static void SetValue(Window window, ulong completed, ulong total)
        {
            if (_taskbarSupported && window != null)
            {
                IntPtr hwnd = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (hwnd != IntPtr.Zero)
                {
                    _taskbarInstance.SetProgressValue(hwnd, completed, total);
                }
            }
        }
    }
}

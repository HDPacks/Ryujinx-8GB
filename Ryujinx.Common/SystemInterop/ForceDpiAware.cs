﻿using Ryujinx.Common.Logging;
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Ryujinx.Common.SystemInterop
{
    public static partial class ForceDpiAware
    {
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetProcessDPIAware();

        private const string X11LibraryName = "libX11.so.6";

        [LibraryImport(X11LibraryName)]
        private static partial IntPtr XOpenDisplay([MarshalAs(UnmanagedType.LPStr)] string display);

        [LibraryImport(X11LibraryName)]
        private static partial IntPtr XGetDefault(IntPtr display, [MarshalAs(UnmanagedType.LPStr)] string program, [MarshalAs(UnmanagedType.LPStr)] string option);

        [LibraryImport(X11LibraryName)]
        private static partial int XDisplayWidth(IntPtr display, int screenNumber);

        [LibraryImport(X11LibraryName)]
        private static partial int XDisplayWidthMM(IntPtr display, int screenNumber);

        [LibraryImport(X11LibraryName)]
        private static partial int XCloseDisplay(IntPtr display);

        private static readonly double _standardDpiScale = 96.0;
        private static readonly double _maxScaleFactor   = 1.25;

        /// <summary>
        /// Marks the application as DPI-Aware when running on the Windows operating system.
        /// </summary>
        public static void Windows()
        {
            // Make process DPI aware for proper window sizing on high-res screens.
            if (OperatingSystem.IsWindowsVersionAtLeast(6))
            {
                SetProcessDPIAware();
            }
        }

        public static double GetActualScaleFactor()
        {
            double userDpiScale = 96.0;

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    userDpiScale = GdiPlusHelper.GetDpiX(IntPtr.Zero);
                }
                else if (OperatingSystem.IsLinux())
                {
                    string xdgSessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE")?.ToLower();

                    if (xdgSessionType == null || xdgSessionType == "x11")
                    {
                        IntPtr display = XOpenDisplay(null);
                        string dpiString = Marshal.PtrToStringAnsi(XGetDefault(display, "Xft", "dpi"));
                        if (dpiString == null || !double.TryParse(dpiString, NumberStyles.Any, CultureInfo.InvariantCulture, out userDpiScale))
                        {
                            userDpiScale = (double)XDisplayWidth(display, 0) * 25.4 / (double)XDisplayWidthMM(display, 0);
                        }
                        XCloseDisplay(display);
                    }
                    else if (xdgSessionType == "wayland")
                    {
                        // TODO
                        Logger.Warning?.Print(LogClass.Application, $"Couldn't determine monitor DPI: Wayland not yet supported");
                    }
                    else
                    {
                        Logger.Warning?.Print(LogClass.Application, $"Couldn't determine monitor DPI: Unrecognised XDG_SESSION_TYPE: {xdgSessionType}");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Warning?.Print(LogClass.Application, $"Couldn't determine monitor DPI: {e.Message}");
            }

            return userDpiScale;
        }

        public static double GetWindowScaleFactor()
        {
            double userDpiScale = GetActualScaleFactor();

            return Math.Min(userDpiScale / _standardDpiScale, _maxScaleFactor);
        }
    }
}

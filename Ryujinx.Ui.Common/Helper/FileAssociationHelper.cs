using Microsoft.Win32;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Ui.Common.Helper
{
    public static partial class FileAssociationHelper
    {
        private static string[] _fileExtensions = new string[] { ".nca", ".nro", ".nso", ".nsp", ".xci" };

        [SupportedOSPlatform("linux")]
        private static string _mimeDbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "mime");

        private const int SHCNE_ASSOCCHANGED = 0x8000000;
        private const int SHCNF_FLUSH        = 0x1000;

        [LibraryImport("shell32.dll", SetLastError = true)]
        public static partial void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        public static bool IsTypeAssociationSupported => (OperatingSystem.IsLinux() || OperatingSystem.IsWindows()) && !ReleaseInformation.IsFlatHubBuild();

        [SupportedOSPlatform("linux")]
        private static bool AreMimeTypesRegisteredLinux() => File.Exists(Path.Combine(_mimeDbPath, "packages", "Ryujinx.xml"));

        [SupportedOSPlatform("linux")]
        private static bool InstallLinuxMimeTypes(bool uninstall = false)
        {
            string installKeyword = uninstall ? "uninstall" : "install";

            if (!AreMimeTypesRegisteredLinux())
            {
                string mimeTypesFile = Path.Combine(ReleaseInformation.GetBaseApplicationDirectory(), "mime", "Ryujinx.xml");
                string additionalArgs = !uninstall ? "--novendor" : "";

                using Process mimeProcess = new();

                mimeProcess.StartInfo.FileName = "xdg-mime";
                mimeProcess.StartInfo.Arguments = $"{installKeyword} {additionalArgs} --mode user {mimeTypesFile}";

                mimeProcess.Start();
                mimeProcess.WaitForExit();

                if (mimeProcess.ExitCode != 0)
                {
                    Logger.Error?.PrintMsg(LogClass.Application, $"Unable to {installKeyword} mime types. Make sure xdg-utils is installed. Process exited with code: {mimeProcess.ExitCode}");

                    return false;
                }

                using Process updateMimeProcess = new();

                updateMimeProcess.StartInfo.FileName = "update-mime-database";
                updateMimeProcess.StartInfo.Arguments = _mimeDbPath;

                updateMimeProcess.Start();
                updateMimeProcess.WaitForExit();

                if (updateMimeProcess.ExitCode != 0)
                {
                    Logger.Error?.PrintMsg(LogClass.Application, $"Could not update local mime database. Process exited with code: {updateMimeProcess.ExitCode}");
                }
            }

            return true;
        }

        [SupportedOSPlatform("windows")]
        private static bool AreMimeTypesRegisteredWindows()
        {
            static bool CheckRegistering(string ext)
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@$"Software\Classes\{ext}");

                if (key is null)
                {
                    return false;
                }

                key.OpenSubKey(@"shell\open\command");

                string keyValue = (string)key.GetValue("");

                return keyValue is not null && (keyValue.Contains("Ryujinx") || keyValue.Contains(AppDomain.CurrentDomain.FriendlyName));
            }

            bool registered = false;

            foreach (string ext in _fileExtensions)
            {
                registered |= CheckRegistering(ext);
            }

            return registered;
        }

        [SupportedOSPlatform("windows")]
        private static bool InstallWindowsMimeTypes(bool uninstall = false)
        {
            static bool RegisterExtension(string ext, bool uninstall = false)
            {
                string keyString = @$"Software\Classes\{ext}";

                if (uninstall)
                {
                    if (!AreMimeTypesRegisteredWindows())
                    {
                        return false;
                    }

                    Registry.CurrentUser.DeleteSubKeyTree(keyString);
                }
                else
                {
                    RegistryKey key = Registry.CurrentUser.CreateSubKey(keyString);
                    if (key is null)
                    {
                        return false;
                    }

                    key.CreateSubKey(@"shell\open\command");

                    key.SetValue("", $"\"{Environment.ProcessPath}\" \"%1\"");
                    key.Close();
                }

                // Notify Explorer the file association has been changed.
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);

                return true;
            }

            bool registered = false;

            foreach (string ext in _fileExtensions)
            {
                registered |= RegisterExtension(ext, uninstall);
            }

            return registered;
        }

        public static bool AreMimeTypesRegistered()
        {
            if (OperatingSystem.IsLinux())
            {
                return AreMimeTypesRegisteredLinux();
            }

            if (OperatingSystem.IsWindows())
            {
                return AreMimeTypesRegisteredWindows();
            }

            // TODO: Add macOS support.

            return false;
        }

        public static bool Install()
        {
            if (OperatingSystem.IsLinux())
            {
                return InstallLinuxMimeTypes();
            }

            if (OperatingSystem.IsWindows())
            {
                return InstallWindowsMimeTypes();
            }

            // TODO: Add macOS support.

            return false;
        }

        public static bool Uninstall()
        {
            if (OperatingSystem.IsLinux())
            {
                return InstallLinuxMimeTypes(true);
            }

            if (OperatingSystem.IsWindows())
            {
                return InstallWindowsMimeTypes(true);
            }

            // TODO: Add macOS support.

            return false;
        }
    }
}
using Ryujinx.Common.Logging;
using System;
using System.IO;

namespace Ryujinx.Common.Configuration
{
    public static class AppDataManager
    {
        public const string DefaultBaseDir = "Ryujinx";
        public const string DefaultPortableDir = "portable";

        // The following 3 are always part of Base Directory
        private const string GamesDir = "games";
        private const string ProfilesDir = "profiles";
        private const string KeysDir = "system";

        public enum LaunchMode
        {
            UserProfile,
            Portable,
            Custom
        }

        public static LaunchMode Mode { get; private set; }

        public static string BaseDirPath { get; private set; }
        public static string GamesDirPath { get; private set; }
        public static string ProfilesDirPath { get; private set; }
        public static string KeysDirPath { get; private set; }
        public static string KeysDirPathUser { get; }

        public const string DefaultNandDir = "bis";
        public const string DefaultSdcardDir = "sdcard";
        private const string DefaultModsDir = "mods";

        public static string CustomModsPath { get; set; }
        public static string CustomSdModsPath {get; set; }
        public static string CustomNandPath { get; set; } // TODO: Actually implement this into VFS
        public static string CustomSdCardPath { get; set; } // TODO: Actually implement this into VFS

        static AppDataManager()
        {
            KeysDirPathUser = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".switch");
        }

        public static void Initialize(string baseDirPath)
        {
            string appDataPath;
            if (OperatingSystem.IsMacOS())
            {
                appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support");
            }
            else
            {
                appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }

            if (appDataPath.Length == 0)
            {
                appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            string userProfilePath = Path.Combine(appDataPath, DefaultBaseDir);
            string portablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultPortableDir);

            if (Directory.Exists(portablePath))
            {
                BaseDirPath = portablePath;
                Mode = LaunchMode.Portable;
            }
            else
            {
                BaseDirPath = userProfilePath;
                Mode = LaunchMode.UserProfile;
            }

            if (baseDirPath != null && baseDirPath != userProfilePath)
            {
                if (!Directory.Exists(baseDirPath))
                {
                    Logger.Error?.Print(LogClass.Application, $"Custom Data Directory '{baseDirPath}' does not exist. Falling back to {Mode}...");
                }
                else
                {
                    BaseDirPath = baseDirPath;
                    Mode = LaunchMode.Custom;
                }
            }

            BaseDirPath = Path.GetFullPath(BaseDirPath); // convert relative paths

            // NOTE: Moves the Ryujinx folder in `~/.config` to `~/Library/Application Support` if one is found
            // and a Ryujinx folder does not already exist in Application Support.
            // Also creates a symlink from `~/.config/Ryujinx` to `~/Library/Application Support/Ryujinx` to preserve backwards compatibility.
            // This should be removed in the future.
            if (OperatingSystem.IsMacOS() && Mode == LaunchMode.UserProfile)
            {
                string oldConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DefaultBaseDir);
                if (Path.Exists(oldConfigPath) && !Path.Exists(BaseDirPath))
                {
                    CopyDirectory(oldConfigPath, BaseDirPath);
                    Directory.Delete(oldConfigPath, true);
                    Directory.CreateSymbolicLink(oldConfigPath, BaseDirPath);
                }
            }

            SetupBasePaths();
        }

        private static void SetupBasePaths()
        {
            Directory.CreateDirectory(BaseDirPath);
            Directory.CreateDirectory(GamesDirPath = Path.Combine(BaseDirPath, GamesDir));
            Directory.CreateDirectory(ProfilesDirPath = Path.Combine(BaseDirPath, ProfilesDir));
            Directory.CreateDirectory(KeysDirPath = Path.Combine(BaseDirPath, KeysDir));
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
            }

            DirectoryInfo[] subDirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                if (file.Name == ".DS_Store")
                {
                    continue;
                }

                file.CopyTo(Path.Combine(destinationDir, file.Name));
            }

            foreach (DirectoryInfo subDir in subDirs)
            {
                CopyDirectory(subDir.FullName, Path.Combine(destinationDir, subDir.Name));
            }
        }

        public static string GetModsPath()   => CustomModsPath ?? Directory.CreateDirectory(Path.Combine(BaseDirPath, DefaultModsDir)).FullName;
        public static string GetSdModsPath() => CustomSdModsPath ?? Directory.CreateDirectory(Path.Combine(BaseDirPath, DefaultSdcardDir, "atmosphere")).FullName;
    }
}
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Installer.Injection
{
    public class SteamPathNotFoundException : Exception
    {
        public SteamPathNotFoundException(string message) : base(message) { }
    }

    public class FileHelper
    {
        private static readonly string Game = "Townscaper";

        private static readonly string SteamRegistry = @"HKEY_CURRENT_USER\Software\Valve\Steam";

        private static readonly string AssemblyLib = "Assembly-CSharp.dll";

        private static readonly string InstallDirectory = $"steamapps/common/{Game}";
        private static readonly string ManagedDirectory = $"{Game}_Data/Managed";

        private static readonly string PluginDirectory = "plugins";

        private static string AbsoluteInstallDirectory;


        public static void Init()
        {
            AbsoluteInstallDirectory = GetAbsoluteInstallDirectory();
        }

        private static string GetAbsoluteInstallDirectory()
        {
            var registry = Registry.GetValue(SteamRegistry, "SteamPath", null);
            if (registry == null)
                throw new SteamPathNotFoundException("Failed to find Steam path in Registry");
            var steamPath = registry.ToString();

            var lines = File.ReadAllLines($"{steamPath}/config/config.vdf");
            var paths = new List<string> { steamPath };
            GetSteamInstallDirectories(paths, lines, 1);

            string path = paths.Find((p)
                => Directory.Exists($"{p}/{InstallDirectory}/{ManagedDirectory}")
                && File.Exists($"{p}/{InstallDirectory}/{ManagedDirectory}/{AssemblyLib}"));
            if (path == null)
                throw new SteamPathNotFoundException($"Failed to find game install directory for {Game}");
            return $"{path}/{InstallDirectory}";
        }

        private static void GetSteamInstallDirectories(List<string> paths, string[] lines, int index)
        {
            foreach (var line in lines.Select((line) => line.Trim()).Where((line) => line.StartsWith($"\"BaseInstallFolder_{index}\""))) {
                string path = line.Split('\t')[2];
                paths.Add(path.Substring(1, path.Length - 2).Replace("\\\\", "/"));
                GetSteamInstallDirectories(paths, lines, index + 1);
            }
        }

        public static string GetManagedDirectory()
        {
            return $"{AbsoluteInstallDirectory}/{ManagedDirectory}";
        }

        public static string GetAssemblyFile()
        {
            return $"{GetManagedDirectory()}/{AssemblyLib}";
        }

        public static string GetAssemblyBackupFile()
        {
            return $"{GetAssemblyFile()}.backup";
        }

        public static string GetPluginDirectory()
        {
            return $"{AbsoluteInstallDirectory}/{PluginDirectory}";
        }

    }
}

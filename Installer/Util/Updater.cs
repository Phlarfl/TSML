using Installer.Injection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Documents;

namespace Installer.Util
{
    public class Updater
    {
        private static readonly int[] VERSION = new int[] { 2, 0, 0 };
        private static readonly string SETTINGS_URL = "https://gist.github.com/Phlarfl/7e19181c4a2d3802d88c73bbd000ebf1/raw";

        private MainWindow Window { get; set; }

        public Updater(MainWindow window)
        {
            Window = window;
        }

        public void CheckInstallerVersion()
        {
            using (var client = new WebClient())
            {
                client.DownloadStringCompleted += (sender, args) =>
                {
                    var json = args.Result.ToString();
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(json);
                    if (settings == null)
                        MessageBox.Show("Failed to parse settings");

                    if (HasNewerVersion(settings.Version, VERSION))
                    {
                        MessageBox.Show("There is a newer version of the TSML Installer, please update to get the latest features");
                        Process.Start("https://phlarfl.github.io/TSML");
                        Window.Close();
                    }
                    if (IsBetaVersion(settings.Version, VERSION))
                    {
                        MessageBox.Show("This is a beta version, use with care");
                    }
                };
                client.DownloadStringAsync(new Uri(SETTINGS_URL));
            }
        }

        public void CheckModVersions(List<PluginItem> installedPlugins)
        {
            using (var client = new WebClient())
            {
                client.DownloadStringCompleted += (sender, args) =>
                {
                    var json = args.Result.ToString();
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(json);
                    if (settings == null)
                        MessageBox.Show("Failed to parse settings");

                    foreach (var plugin in settings.ModList)
                    {
                        Window.LbxMods.Items.Add(plugin);
                        if (HasNewerVersion(plugin.Version, installedPlugins.Find((item) => item.Name.Equals(plugin.Name))?.Version))
                            MessageBox.Show($"Installed version of {plugin.Name} is outdated, consider installing the latest version: {plugin.Version[0]}.{plugin.Version[1]}.{plugin.Version[2]}");
                    }

                    Window.PgbLoad.IsIndeterminate = false;
                    Window.PgbLoad.Value = 0;
                    Window.RefreshButtonStates();
                };
                client.DownloadStringAsync(new Uri(SETTINGS_URL));
            }
        }

        public void DownloadMod(PluginItem plugin, Action success, Action error)
        {
            using (var client = new WebClient())
            {
                try
                {
                    client.DownloadFileCompleted += (sender, args) =>
                    {
                        int downloaded = 0;
                        foreach (Dependency dependency in plugin.Dependencies)
                        {
                            DownloadDependency(dependency, () =>
                            {
                                downloaded++;
                                if (downloaded == plugin.Dependencies.Length)
                                    success();
                            }, error);
                        }
                    };
                    client.DownloadFileAsync(new Uri(plugin.Download), $"{FileHelper.GetPluginDirectory()}/{plugin.Name}v{plugin.Version[0]}.{plugin.Version[1]}.{plugin.Version[2]}.dll");
                } catch
                {
                    error();
                }
            }
        }

        private void DownloadDependency(Dependency dependency, Action success, Action error)
        {
            using (var client = new WebClient())
            {
                try
                {
                    client.DownloadFileCompleted += (sender, args) =>
                    {
                        success();
                    };
                    client.DownloadFileAsync(new Uri(dependency.Url), $"{FileHelper.GetManagedDirectory()}/{dependency.Name}.dll");
                } catch
                {
                    error();
                }
            }
        }

        private bool HasNewerVersion(int[] newVersion, int[] version)
        {
            return version != null && (newVersion[0] > version[0]
                || (newVersion[0] == version[0] && newVersion[1] > version[1])
                || (newVersion[0] == version[0] && newVersion[1] == version[1] && newVersion[2] > version[2]));
        }

        private bool IsBetaVersion(int[] newVersion, int[] version)
        {
            return version != null && (newVersion[0] < version[0]
                || (newVersion[0] == version[0] && newVersion[1] < version[1])
                || (newVersion[0] == version[0] && newVersion[1] == version[1] && newVersion[2] < version[2]));
        }
    }
}

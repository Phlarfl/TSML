using Installer.Injection;
using Installer.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using TSML;

namespace Installer
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<Control, bool> States = new Dictionary<Control, bool>();

        private Updater Updater { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Updater = new Updater(this);
            Updater.CheckInstallerVersion();

            try
            {
                FileHelper.Init();
            } catch (SteamPathNotFoundException e)
            {
                MessageBox.Show($"{e.Message}\r\nPlease make sure you've installed the game first");
                Close();
                return;
            }

            RefreshButtonStates();
            RefreshInstalled();
            RefreshMods();
        }

        private void BtnInstallModLoader_Click(object sender, RoutedEventArgs e)
        {
            ChangeState(false);
            PgbLoad.Value = 1;
            PgbLoad.IsIndeterminate = false;
            RefreshButtonStates();

            new Thread(() =>
            {
                InjectHelper.InstallModLoader(this);
                Dispatcher.Invoke(() =>
                {
                    PgbLoad.Value = 0;
                    PgbLoad.IsIndeterminate = false;
                    RefreshButtonStates();
                    RefreshInstalled();
                });
            }).Start();
        }

        private void BtnUninstallModLoader_Click(object sender, RoutedEventArgs e)
        {
            ChangeState(false);

            var backup = FileHelper.GetAssemblyBackupFile();
            if (File.Exists(backup))
            {
                File.Copy(backup, FileHelper.GetAssemblyFile(), true);
                File.Delete(backup);
            } else
            {
                MessageBox.Show("Failed to find original game files, please reinstall the game to uninstall Townscaper Mod Loader");
                Close();
                return;
            }

            ReturnState();
            RefreshButtonStates();

            LbxInstalled.Items.Clear();
        }

        private void BtnInstallMod_Click(object sender, RoutedEventArgs e)
        {
            var plugin = LbxMods.SelectedItem as PluginItem;
            if (plugin == null)
            {
                MessageBox.Show("You must select a plugin to install");
                return;
            }
            if (!Directory.Exists(FileHelper.GetPluginDirectory()))
                Directory.CreateDirectory(FileHelper.GetPluginDirectory());
            PgbLoad.IsIndeterminate = true;
            RefreshButtonStates();
            Updater.DownloadMod(plugin, () =>
            {
                PgbLoad.Value = 0;
                PgbLoad.IsIndeterminate = false;
                RefreshButtonStates();
                RefreshInstalled();
            }, () =>
            {
                PgbLoad.Value = 0;
                PgbLoad.IsIndeterminate = false;
                RefreshButtonStates();
                MessageBox.Show($"Failed to download {plugin.Name} v{plugin.Version[0]}.{plugin.Version[1]}.{plugin.Version[2]}");
            });
        }

        private void BtnUninstallMod_Click(object sender, RoutedEventArgs e)
        {
            var plugin = LbxInstalled.SelectedItem as PluginItem;
            if (plugin == null)
            {
                MessageBox.Show("You must select an plugin to uninstall");
                return;
            }
            if (File.Exists(plugin.Download))
                File.Delete(plugin.Download);
            if (LbxInstalled.Items.Count > 0) LbxInstalled.SelectedIndex = Math.Min(Math.Max(0, LbxInstalled.SelectedIndex), LbxInstalled.Items.Count - 1);
            if (LbxInstalled.SelectedIndex == -1) BtnUninstallMod.IsEnabled = false;
            RefreshInstalled();
        }

        private void BtnRefreshMods_Click(object sender, RoutedEventArgs e)
        {
            RefreshMods();
        }

        private void BtnRefreshInstalled_Click(object sender, RoutedEventArgs e)
        {
            RefreshInstalled();
        }

        private void LbxMods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnInstallMod.IsEnabled = LbxMods.SelectedItem != null;
        }

        private void LbxInstalled_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnUninstallMod.IsEnabled = LbxInstalled.SelectedItem != null;
        }

        public void RefreshButtonStates()
        {
            if (PgbLoad.Value % 100 != 0 || PgbLoad.IsIndeterminate)
            {
                BtnInstallModLoader.IsEnabled = BtnInstallMod.IsEnabled = BtnUninstallModLoader.IsEnabled = BtnUninstallMod.IsEnabled = BtnRefreshInstalled.IsEnabled = BtnRefreshMods.IsEnabled = LbxInstalled.IsEnabled = LbxMods.IsEnabled = false;
                return;
            }
            var Installed = InjectHelper.IsModLoaderInstalled();
            BtnInstallModLoader.IsEnabled = !Installed;
            BtnUninstallModLoader.IsEnabled = BtnRefreshMods.IsEnabled = BtnRefreshInstalled.IsEnabled = BtnInstallMod.IsEnabled = BtnUninstallMod.IsEnabled = LbxMods.IsEnabled = LbxInstalled.IsEnabled = Installed;
            if (LbxInstalled.Items.Count == 0)
                BtnUninstallMod.IsEnabled = false;
            if (LbxMods.Items.Count == 0)
                BtnInstallMod.IsEnabled = false;
        }

        private void ChangeState(bool state)
        {
            var controls = new List<Control>
            {
                BtnInstallModLoader,
                BtnUninstallModLoader,

                BtnRefreshMods,
                BtnRefreshInstalled,
                BtnInstallMod,
                BtnUninstallMod,

                LbxMods,
                LbxInstalled
            };

            States.Clear();
            foreach (var control in controls)
            {
                States.Add(control, control.IsEnabled);
                control.IsEnabled = state;
            }
        }

        private void ReturnState()
        {
            foreach (var control in States.Keys)
                control.IsEnabled = States[control];
        }

        private void RefreshMods()
        {
            LbxMods.Items.Clear();
            PgbLoad.IsIndeterminate = true;
            RefreshButtonStates();

            List<PluginItem> pluginItems = new List<PluginItem>();
            foreach (PluginItem plugin in LbxInstalled.Items)
                pluginItems.Add(plugin);
            Updater.CheckModVersions(pluginItems);
        }

        private void RefreshInstalled()
        {
            LbxInstalled.Items.Clear();

            var pluginDirectory = FileHelper.GetPluginDirectory();
            if (Directory.Exists(pluginDirectory))
            {
                var files = Directory.GetFiles(pluginDirectory, $"*.dll", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    var plugin = InjectHelper.GetPlugin(file);
                    if (plugin != null)
                        LbxInstalled.Items.Add(plugin);
                    else LbxInstalled.Items.Add(new PluginItem("Unknown Plugin", null, null, file, false, new Dependency[0]));
                }
            }
        }
    }
}

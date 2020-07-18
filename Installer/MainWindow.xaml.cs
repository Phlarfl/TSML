using ILRepacking;
using Microsoft.Win32;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string GAME = "Townscaper";
        private readonly int[] VERSION = new int[] { 1, 1, 0 };

        private const string SETTINGS_URL = "https://gist.github.com/Phlarfl/7e19181c4a2d3802d88c73bbd000ebf1/raw";
        private const string STEAM_REGISTRY = @"HKEY_CURRENT_USER\Software\Valve\Steam";
        private const string STEAM_CONFIG = "config/config.vdf";

        private const string ASSEMBLY_LIB = "Assembly-CSharp.dll";

        private readonly string INSTALL_DIRECTORY = $"steamapps/common/{GAME}";
        private readonly string MANAGED_DIRECTORY = $"{GAME}_Data/Managed";

        private const string PLUGIN_DIRECTORY = "plugins";
        private const string PLUGIN_EXT = "dll";

        private bool Reinstalling = false;

        private string AbsoluteInstallDirectory;

        private Dictionary<Control, bool> States = new Dictionary<Control, bool>();

        public MainWindow()
        {
            InitializeComponent();

            using (var client = new WebClient())
            {
                client.DownloadStringCompleted += OnSettingsDownloadComplete;
                client.DownloadStringAsync(new Uri(SETTINGS_URL));
            }

            AbsoluteInstallDirectory = GetAbsoluteInstallDirectory();
            if (AbsoluteInstallDirectory == null)
            {
                MessageBox.Show($"Failed to find install directory for {GAME}. Please make sure you've installed the game first");
                Close();
                return;
            }

            RefreshButtonStates();
            RefreshMods();
            RefreshInstalled();
        }

        private void BtnInstallModLoader_Click(object sender, RoutedEventArgs e)
        {
            ChangeState(false);
            PgbLoad.Value = 25;
            RefreshButtonStates();

            new Thread(() =>
            {
                Inject();
            }).Start();

            PgbLoad.IsIndeterminate = false;
            PgbLoad.Value = 0;
            RefreshButtonStates();
        }

        private void BtnUninstallModLoader_Click(object sender, RoutedEventArgs e)
        {
            ChangeState(false);
            
            File.Copy(GetAssemblyBackupLocation(), GetAssemblyLocation(), true);
            if (File.Exists(GetAssemblyBackupLocation()))
                File.Delete(GetAssemblyBackupLocation());
            if (Directory.Exists(GetPluginDirectory()))
                Directory.Delete(GetPluginDirectory(), true);

            ReturnState();
            RefreshButtonStates();

            LbxInstalled.Items.Clear();
            Reinstalling = true;
        }

        private void BtnInstallMod_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnUninstallMod_Click(object sender, RoutedEventArgs e)
        {

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

        private string GetManagedLocation()
        {
            return $"{AbsoluteInstallDirectory}/{MANAGED_DIRECTORY}";
        }

        private string GetAssemblyLocation()
        {
            return $"{GetManagedLocation()}/{ASSEMBLY_LIB}";
        }

        private string GetAssemblyBackupLocation()
        {
            return $"{GetAssemblyLocation()}.bac";
        }

        private string GetPluginDirectory()
        {
            return $"{AbsoluteInstallDirectory}/${PLUGIN_DIRECTORY}";
        }

        private string GetAbsoluteInstallDirectory()
        {
            string steam = Registry.GetValue($"{STEAM_REGISTRY}", "SteamPath", null)?.ToString();
            if (steam == null)
                return null;

            List<string> paths = new List<string>
            {
                steam
            };

            string[] lines = File.ReadAllLines($"{steam}/{STEAM_CONFIG}");
            VDFPopulate(paths, lines, 1);
            foreach (var path in paths)
                if (Directory.Exists($"{path}/{INSTALL_DIRECTORY}/{MANAGED_DIRECTORY}") && File.Exists($"{path}/{INSTALL_DIRECTORY}/{MANAGED_DIRECTORY}/{ASSEMBLY_LIB}"))
                    return $"{path}/{INSTALL_DIRECTORY}";
            return null;
        }

        private void VDFPopulate(List<string> paths, string[] lines, int index)
        {
            foreach (string line in lines)
                if (line.Trim().StartsWith($"\"BaseInstallFolder_{index}\""))
                {
                    string path = line.Trim().Split('\t')[2];
                    paths.Add(path.Substring(1, path.Length - 2).Replace("\\\\", "/"));
                    VDFPopulate(paths, lines, index + 1);
                }
        }

        private bool IsModLoaderInstalled()
        {
            if (!File.Exists(GetAssemblyLocation()))
                return false;

            AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(GetAssemblyLocation());
            ModuleDefinition mod = asm.MainModule;

            return Util.GetDefinition(mod.Types, "TSML.Core", true) != null;
        }

        private void RefreshButtonStates()
        {
            if (PgbLoad.Value != 0 || PgbLoad.IsIndeterminate)
            {
                BtnInstallModLoader.IsEnabled = BtnInstallMod.IsEnabled = BtnUninstallModLoader.IsEnabled = BtnUninstallMod.IsEnabled = BtnRefreshInstalled.IsEnabled = BtnRefreshMods.IsEnabled = LbxInstalled.IsEnabled = LbxMods.IsEnabled = false;
                return;
            }
            bool Installed = IsModLoaderInstalled();
            BtnInstallModLoader.IsEnabled = !Installed;
            BtnUninstallModLoader.IsEnabled = BtnRefreshMods.IsEnabled = BtnRefreshInstalled.IsEnabled = BtnInstallMod.IsEnabled = BtnUninstallMod.IsEnabled = LbxMods.IsEnabled = LbxInstalled.IsEnabled = Installed;
            if (LbxInstalled.Items.Count == 0)
                BtnUninstallMod.IsEnabled = false;
            if (LbxMods.Items.Count == 0)
                BtnInstallMod.IsEnabled = false;
        }

        private void ChangeState(bool state)
        {
            List<Control> controls = new List<Control>();

            controls.Add(BtnInstallModLoader);
            controls.Add(BtnUninstallModLoader);

            controls.Add(BtnRefreshMods);
            controls.Add(BtnRefreshInstalled);
            controls.Add(BtnInstallMod);
            controls.Add(BtnUninstallMod);

            controls.Add(LbxMods);
            controls.Add(LbxInstalled);

            States.Clear();
            foreach (Control control in controls)
            {
                States.Add(control, control.IsEnabled);
                control.IsEnabled = state;
            }
        }

        private void ReturnState()
        {
            foreach (Control control in States.Keys)
                control.IsEnabled = States[control];
        }

        private void RefreshMods()
        {
            LbxMods.Items.Clear();
            PgbLoad.IsIndeterminate = true;
            RefreshButtonStates();

            using (var client = new WebClient())
            {
                client.DownloadStringCompleted += OnStringDownloadComplete;
                client.DownloadStringAsync(new Uri(SETTINGS_URL));
            }
        }

        private void RefreshInstalled()
        {
            LbxInstalled.Items.Clear();

            if (Directory.Exists(GetPluginDirectory()))
            {
                string[] files = Directory.GetFiles(GetPluginDirectory(), $"*.{PLUGIN_EXT}", SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                    LbxInstalled.Items.Add(file.Substring(file.LastIndexOf('\\') + 1).Replace(".dll", ""));
            }
        }

        private void Inject()
        {
            List<string> managed = Directory.GetFiles(GetManagedLocation()).Where(x => (x.Contains("UnityEngine") || x.Contains("AmplifyMotion") || x.Contains("Assembly-CSharp") || x.Contains("Mono.Security")) && x.EndsWith(".dll")).ToList();
            foreach (string file in managed)
                if (!Reinstalling || !File.Exists(file.Substring(file.LastIndexOf("\\") + 1)))
                    File.Copy(file, file.Substring(file.LastIndexOf("\\") + 1), true);

            Dispatcher.Invoke(new Action(delegate ()
            {
                PgbLoad.Value = 25;
                RefreshButtonStates();
            }));

            string dll = GetAssemblyLocation();
            string dllBackup = GetAssemblyBackupLocation();
            Merge(dll, dllBackup, $"./TSML.dll");
            Inject(dll);
        }

        private void Inject(string dll)
        {
            Assembly asm = Assembly.Load(File.ReadAllBytes(dll));

            AssemblyDefinition asmDef = AssemblyDefinition.ReadAssembly(GetAssemblyLocation());
            ModuleDefinition mainModule = asmDef.MainModule;

            InjectionMethod imOnEnable = new InjectionMethod(mainModule, "Placemaker.BootMaster", "OnEnable");
            MethodReference mrInit = mainModule.ImportReference(asm.GetType("TSML.Core").GetMethod("Init", new Type[] { }));
            imOnEnable.MethodDef.Body.Instructions.Insert(3, imOnEnable.MethodDef.Body.GetILProcessor().Create(OpCodes.Call, mrInit));

            string newDll = $"{dll}.new";
            asmDef.Write(newDll);
            asmDef.Dispose();

            File.Copy(newDll, dll, true);
            File.Delete(newDll);

            Dispatcher.Invoke(new Action(delegate ()
            {
                PgbLoad.IsIndeterminate = false;
                PgbLoad.Value = 0;
                RefreshButtonStates();
            }));
        }

        private void Merge(string dll, string dllBackup, string modloader)
        {
            AssemblyDefinition asmDef = AssemblyDefinition.ReadAssembly(dll);
            ModuleDefinition mainModule = asmDef.MainModule;

            if (Util.GetDefinition(mainModule.Types, "TSML.Core", true) == null)
                File.Copy(dll, dllBackup, true);

            asmDef.Dispose();

            var options = new RepackOptions
            {
                OutputFile = dll,
                InputAssemblies = new[]
                {
                    dllBackup, modloader
                },
                SearchDirectories = new List<string>().AsEnumerable(),
                TargetKind = ILRepack.Kind.Dll
            };

            var repack = new ILRepack(options);
            repack.Repack();

            Dispatcher.Invoke(new Action(delegate ()
            {
                PgbLoad.Value = 75;
                RefreshButtonStates();
            }));
        }

        private void OnStringDownloadComplete(object sender, DownloadStringCompletedEventArgs e)
        {
            string json = e.Result.ToString();
            Settings settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(json);
            if (settings == null)
                MessageBox.Show("Failed to parse mod list");
            foreach (Plugin plugin in settings.modlist)
                LbxMods.Items.Add(plugin);

            PgbLoad.IsIndeterminate = false;
            PgbLoad.Value = 0;
            RefreshButtonStates();
        }

        private void OnFileDownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            PgbLoad.IsIndeterminate = false;
            PgbLoad.Value = 0;
            RefreshButtonStates();
            RefreshInstalled();
        }

        private void OnSettingsDownloadComplete(object sender, DownloadStringCompletedEventArgs e)
        {
            string json = e.Result.ToString();
            Settings settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(json);
            if (settings == null)
                MessageBox.Show("Failed to parse settings");

            if (settings.version[0] > VERSION[0]
                || (settings.version[0] == VERSION[0] && settings.version[1] > VERSION[1])
                || (settings.version[0] == VERSION[0] && settings.version[1] == VERSION[1] && settings.version[2] > VERSION[2]))
            {
                MessageBox.Show("There is a newer version of the TSML Installer, please update to get the latest features");
                Process.Start("https://phlarfl.github.io/TSML");
                Close();
            }
        }
    }
}

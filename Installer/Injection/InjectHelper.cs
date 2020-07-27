using ILRepacking;
using Installer.Injection.Injectors;
using Installer.Util;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TSML;

namespace Installer.Injection
{
    public class InjectHelper
    {
        public static PluginItem GetPlugin(string file)
        {
            AssemblyDefinition assemblyDefinition;
            try
            {
                assemblyDefinition = AssemblyDefinition.ReadAssembly(file);
            } catch
            {
                return null;
            }
            var moduleDefinition = assemblyDefinition.MainModule;
            foreach (var type in moduleDefinition.Types)
            {
                foreach (var attribute in type.CustomAttributes)
                {
                    if (attribute.AttributeType.ToString() == typeof(PluginDescriptor).ToString()
                        && typeof(PluginDescriptor).GetConstructors().Count() == 1
                        && attribute.ConstructorArguments.Count == typeof(PluginDescriptor).GetConstructors()[0].GetParameters().Count())
                    {
                        var arg0 = attribute.ConstructorArguments[0].Value;
                        var arg1 = attribute.ConstructorArguments[1].Value;
                        var arg2 = attribute.ConstructorArguments[2].Value;
                        if (arg0 is string name
                            && arg1 is CustomAttributeArgument[] authorsArray
                            && arg2 is CustomAttributeArgument[] versionArray)
                        {
                            var authors = new string[authorsArray.Length];
                            var version = new int[versionArray.Length];
                            for (var i = 0; i < authorsArray.Length; i++) authors[i] = authorsArray[i].Value.ToString();
                            for (var i = 0; i < versionArray.Length; i++) version[i] = int.Parse(versionArray[i].Value.ToString());
                            assemblyDefinition.Dispose();
                            return new PluginItem(name, authors, version, file, false, new Dependency[0]);
                        }
                    }
                }
            }
            return null;
        }

        public static bool IsModLoaderInstalled()
        {
            if (!File.Exists(FileHelper.GetAssemblyFile()))
                return false;

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(FileHelper.GetAssemblyFile());
            var moduleDefinition = assemblyDefinition.MainModule;

            var isInstalled = Util.Util.GetDefinition(moduleDefinition.Types, "TSML.Core", true) != null;
            assemblyDefinition.Dispose();
            return isInstalled;
        }

        public static void InstallModLoader(MainWindow window)
        {
            window.Dispatcher.Invoke(() =>
            {
                window.PgbLoad.Value = 0;
            });
            var dll = FileHelper.GetAssemblyFile();
            Repack();
            window.Dispatcher.Invoke(() =>
            {
                window.PgbLoad.Value = 33;
            });
            Merge(dll);
            window.Dispatcher.Invoke(() =>
            {
                window.PgbLoad.Value = 66;
            });
            Inject(dll);
            window.Dispatcher.Invoke(() =>
            {
                window.PgbLoad.Value = 100;
            });
        }

        private static void Repack()
        {
            var managedLibraries = Directory.GetFiles(FileHelper.GetManagedDirectory())
                .Where((file) 
                    => (file.Contains("UnityEngine")
                        || file.Contains("Assembly-CSharp")
                        || file.Contains("Mono.Security"))
                    && file.EndsWith(".dll"));

            foreach (var library in managedLibraries) {
                var path = library.Substring(library.LastIndexOf("\\") + 1);
                if (!File.Exists(path))
                    File.Copy(library, path, true);
            }
        }
        
        private static void Merge(string dll)
        {
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(dll);
            var moduleDefinition = assemblyDefinition.MainModule;

            var backup = $"{dll}.backup";
            if (Util.Util.GetDefinition(moduleDefinition.Types, "TSML.Core", true) == null)
                File.Copy(dll, backup, true);

            assemblyDefinition.Dispose();

            var options = new RepackOptions
            {
                OutputFile = dll,
                InputAssemblies = new[]
                {
                    backup, "./TSML.dll"
                },
                SearchDirectories = new List<string>().AsEnumerable(),
                TargetKind = ILRepack.Kind.Dll
            };

            new ILRepack(options).Repack();
        }

        private static void Inject(string dll)
        {
            var assembly = Assembly.Load(File.ReadAllBytes(dll));
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(dll);

            var injectors = new List<Injector>() {
                new BootMasterInjector(),
                new GroundClickerInjector()
            };

            foreach (var injector in injectors)
                injector.Process(assembly, assemblyDefinition);

            var @new = $"{dll}.new";
            assemblyDefinition.Write(@new);
            assemblyDefinition.Dispose();

            File.Copy(@new, dll, true);
            File.Delete(@new);
        }
    }
}

using Placemaker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TSML
{
    public class Core : MonoBehaviour
    {

        private const string PLUGIN_DIR = @".\plugins";
        private const string PLUGIN_EXT = "dll";

        [DllImport("kernel32")]
        private static extern bool AllocConsole();

        public static void Init()
        {
            BootMaster.instance.gameObject.AddComponent<Core>();
        }

        public void Start()
        {
            AllocConsole();
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            });
            Console.SetError(new StreamWriter(Console.OpenStandardError())
            {
                AutoFlush = true
            });
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));

            Console.WriteLine("TSML Initialized");

            LoadPlugins();
        }

        private void LoadPlugins()
        {
            Console.WriteLine("Loading plugins");

            string[] files = Directory.GetFiles(PLUGIN_DIR, $"*.{PLUGIN_EXT}", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                Console.WriteLine("No plugins found");
                return;
            }

            Console.WriteLine($"Found {files.Length} plugins");

            List<Type> plugins = new List<Type>();
            foreach (string file in files)
            {
                Console.WriteLine($"Loading plugin {file}");
                Assembly asm;
                try
                {
                    asm = Assembly.Load(File.ReadAllBytes(file));
                } catch (Exception e)
                {
                    Console.WriteLine($"Failed to load plugin {file}");
                    Console.WriteLine(e);
                    continue;
                }
                Type[] types;
                try
                {
                    types = asm.GetExportedTypes();
                } catch (ReflectionTypeLoadException e)
                {
                    Console.WriteLine($"Load Exceptions: {e.LoaderExceptions.Length}");
                    foreach (Exception loaderEx in e.LoaderExceptions)
                        Console.WriteLine(loaderEx);
                    continue;
                } catch (Exception e)
                {
                    Console.WriteLine($"Failed to get exported types for plugin {file}");
                    Console.WriteLine(e);
                    continue;
                }
                foreach (var type in types)
                    if (typeof(Plugin).IsAssignableFrom(type))
                        plugins.Add(type);
            }
            Console.WriteLine($"Adding plugin components ({plugins.Count})");
            foreach (Type plugin in plugins)
                gameObject.AddComponent(plugin);
        }

    }
}

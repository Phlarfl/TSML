using Multiplayer.Gui;
using Multiplayer.Network;
using Placemaker;
using System;
using TSML;
using UnityEngine;

namespace Multiplayer
{
    [PluginDescriptor("Multiplayer", new[] { "Phlarfl" }, new[] { 1, 0, 0 })]
    public class Multiplayer : Plugin
    {
        public static Multiplayer GetInstance()
        {
            return BootMaster.instance.GetComponent<Multiplayer>();
        }

        public static readonly string PLUGIN_IDENTIFIER = "tsml-plugin-multiplayer";

        public Server Server { get; set; }
        public Client Client { get; set; }

        public void Start()
        {
            Console.WriteLine("Multiplayer plugin loaded");
            Application.runInBackground = true;

            Server = new Server();
            Client = new Client();

            gameObject.AddComponent<OverlayMenu>();
        }

        public void Update()
        {
            Server?.Update();
        }

    }
}

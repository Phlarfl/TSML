using UnityEngine;

namespace Multiplayer.Gui
{
    public class OverlayMenu : MonoBehaviour
    {
        private Multiplayer Multiplayer { get; set; }

        private string Name { get; set; }
        private Vector3 Color { get; set; }

        private string IP { get; set; }
        private string Port { get; set; }
        private string MaxConnections { get; set; }
        private Rect WindowRect { get; set; }
        private bool ShowWindow { get; set; }

        public void Start()
        {
            Multiplayer = Multiplayer.GetInstance();

            Name = "Player";
            Color = new Vector3(0, 0, 0);

            IP = "127.0.0.1";
            Port = "4444";
            MaxConnections = "100";

            WindowRect = new Rect(16, 16, Screen.width - 32, Screen.height - 64);
            ShowWindow = false;
        }

        public void OnGUI()
        {
            WindowRect = new Rect(16, 16, Screen.width - 32, Screen.height - 64);
            ShowWindow = GUI.Toggle(new Rect(16, Screen.height - 16 - 24, 150, 24), ShowWindow, $"{(ShowWindow ? "Close" : "Open")} Multiplayer", "Button");
            if (ShowWindow)
                WindowRect = GUI.Window(0, WindowRect, GetMultiplayerMenu, "Multiplayer");
        }

        private void GetMultiplayerMenu(int id)
        {
            // server
            Rect ServerGroupRect = new Rect(16, 32, WindowRect.width - 32, (WindowRect.height / 2f) - 32);
            GUI.BeginGroup(ServerGroupRect);
            {
                // configuration
                Rect ConfigureRect = new Rect(0, 0, 200, ServerGroupRect.height);
                GUI.BeginGroup(ConfigureRect);
                {
                    Vector2 PortLabel = CreateLabel(new GUIContent("Server Port"), 0, 0, 1, 1);
                    Rect PortRect = new Rect(PortLabel.x + 8, 0, ConfigureRect.width - PortLabel.x - 8, 24);
                    Port = GUI.TextField(PortRect, Port);

                    Vector2 MaxConnectionsLabel = CreateLabel(new GUIContent("Max Connections"), 0, PortRect.y + PortRect.height + 8, 1, 1);
                    Rect MaxConnectionsRect = new Rect(MaxConnectionsLabel.x + 8, PortRect.y + PortRect.height + 8, ConfigureRect.width - MaxConnectionsLabel.x - 8, 24);
                    MaxConnections = GUI.TextField(MaxConnectionsRect, MaxConnections);

                    Rect ChangeStatusButtonRect = new Rect(0, MaxConnectionsRect.y + MaxConnectionsRect.height + 8, ConfigureRect.width, 24);
                    if (GUI.Button(ChangeStatusButtonRect, $"{(!Multiplayer.Server.IsRunning() ? "Start" : "Stop")} Server"))
                    {
                        if (!Multiplayer.Server.IsRunning())
                            Multiplayer.Server.Init(int.Parse(Port), int.Parse(MaxConnections));
                        else
                            Multiplayer.Server.Shutdown();
                    }
                }
                GUI.EndGroup();

                // output
                if (Multiplayer.Server != null)
                    GUI.TextArea(new Rect(ConfigureRect.x + ConfigureRect.width + 16, 0, ServerGroupRect.width - ConfigureRect.x - ConfigureRect.width - 16, ServerGroupRect.height), string.Join("\n", Multiplayer.Server.Output));
            }
            GUI.EndGroup();

            // client
            Rect ClientGroupRect = new Rect(16, ServerGroupRect.y + ServerGroupRect.height + 16, WindowRect.width - 32, (WindowRect.height / 2f) - 32);
            GUI.BeginGroup(ClientGroupRect);
            {
                // configuration
                Rect ConfigureRect = new Rect(0, 0, 200, ClientGroupRect.height);
                GUI.BeginGroup(ConfigureRect);
                {
                    Vector2 NameLabel = CreateLabel(new GUIContent("Name"), 0, 0, 1, 1);
                    Rect NameRect = new Rect(NameLabel.x + 8, 0, ConfigureRect.width - NameLabel.x - 8, 24);
                    Name = GUI.TextField(NameRect, Name);

                    Vector2 IPLabel = CreateLabel(new GUIContent("IP"), 0, NameRect.y + NameRect.height + 8, 1, 1);
                    Rect IPRect = new Rect(IPLabel.x + 8, NameRect.y + NameRect.height + 8, ConfigureRect.width - IPLabel.x - 8, 24);
                    IP = GUI.TextField(IPRect, IP);

                    Vector2 PortLabel = CreateLabel(new GUIContent("Port"), 0, IPRect.y + IPRect.height + 8, 1, 1);
                    Rect PortRect = new Rect(PortLabel.x + 8, IPRect.y + IPRect.height + 8, ConfigureRect.width - PortLabel.x - 8, 24);
                    Port = GUI.TextField(PortRect, Port);

                    Rect ChangeStatusButtonRect = new Rect(0, PortRect.y + PortRect.height + 8, ConfigureRect.width, 24);
                    if (GUI.Button(ChangeStatusButtonRect, $"{(!Multiplayer.Client.IsRunning() ? "Start" : "Stop")} Client"))
                    {
                        if (!Multiplayer.Client.IsRunning())
                            Multiplayer.Client.Init(Name, IP, int.Parse(Port));
                        else
                            Multiplayer.Client.Shutdown();
                    }
                }
                GUI.EndGroup();

                // output
                if (Multiplayer.Client != null)
                    GUI.TextArea(new Rect(ConfigureRect.x + ConfigureRect.width + 16, 0, ClientGroupRect.width - ConfigureRect.x - ConfigureRect.width - 16, ClientGroupRect.height), string.Join("\n", Multiplayer.Client.Output));
            }
            GUI.EndGroup();
        }

        private Vector2 CreateLabel(GUIContent content, float x, float y, int xAlign, int yAlign)
        {
            Vector2 size = GUI.skin.label.CalcSize(content);
            GUI.Label(new Rect(x - (size.x / 2f) + ((size.x / 2f) * xAlign), y - (size.y / 2f) + ((size.y / 2f) * yAlign), size.x, size.y), content);
            return size;
        }
    }
}

﻿using Lidgren.Network;
using Multiplayer.Network.Packet;
using Multiplayer.Network.Packet.World;
using Placemaker;
using System;
using System.Collections.Generic;

namespace Multiplayer.Network
{
    public class Server
    {
        public List<string> Output = new List<string>();

        private NetServer server;

        private void Print(string message)
        {
            Output.Insert(0, message);
        }

        public void Init(int port, int maxConnections)
        {
            Print($"Starting server on port {port} with {maxConnections} maximum connections");

            var config = new NetPeerConfiguration(Multiplayer.PLUGIN_IDENTIFIER)
            {
                MaximumConnections = maxConnections,
                Port = port
            };

            server = new NetServer(config);
            server.Start();
        }

        public bool IsRunning()
        {
            return server != null && server.Status != NetPeerStatus.NotRunning;
        }

        public void Shutdown()
        {
            server?.Shutdown("Server closing");
        }

        public void Update()
        {
            if (server != null)
            {
                NetIncomingMessage incoming;
                while ((incoming = server.ReadMessage()) != null)
                {
                    switch (incoming.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            {
                                var message = incoming.ReadString();
                                Print(message);
                            }
                            break;

                        case NetIncomingMessageType.StatusChanged:
                            {
                                var status = (NetConnectionStatus)incoming.ReadByte();
                                var reason = incoming.ReadString();

                                switch (status)
                                {
                                    case NetConnectionStatus.Connected:
                                        {
                                            var hailMessage = incoming.SenderConnection.RemoteHailMessage;
                                            var name = hailMessage.ReadString();
                                            Print($"{name} joined the game");

                                            if (BootMaster.instance.worldMaster && BootMaster.instance.worldMaster.state == WorldMaster.State.Done)
                                            {
                                                var packetWorld = new PacketWorld(server)
                                                {
                                                    WorldData = BootMaster.instance.worldMaster.GetSaveString()
                                                };
                                                var outgoing = packetWorld.Serialize();
                                                incoming.SenderConnection.SendMessage(outgoing, NetDeliveryMethod.ReliableOrdered, 0);
                                            }
                                        }
                                        break;
                                    case NetConnectionStatus.Disconnected:
                                        {
                                            Print($"{NetUtility.ToHexString(incoming.SenderConnection.RemoteUniqueIdentifier)} {status}: {reason}");
                                        }
                                        break;
                                }
                            }
                            break;
                        case NetIncomingMessageType.Data:
                            {
                                var packetType = (PacketType)incoming.ReadByte();
                                switch (packetType)
                                {
                                    case PacketType.WORLD_DATA:
                                        {
                                            var packet = new PacketWorld(server);
                                            packet.Deserialize(incoming);
                                        }
                                        break;

                                    case PacketType.ADD_VOXEL:
                                        {
                                            var packet = new PacketAddVoxel(server);
                                            packet.Deserialize(incoming);

                                            var others = server.Connections;
                                            others.Remove(incoming.SenderConnection);

                                            if (others.Count > 0)
                                                server.SendMessage(packet.Serialize(), others, NetDeliveryMethod.ReliableOrdered, 0);
                                        }
                                        break;

                                    case PacketType.REMOVE_VOXEL:
                                        {
                                            var packet = new PacketRemoveVoxel(server);
                                            packet.Deserialize(incoming);

                                            var others = server.Connections;
                                            others.Remove(incoming.SenderConnection);

                                            if (others.Count > 0)
                                                server.SendMessage(packet.Serialize(), others, NetDeliveryMethod.ReliableOrdered, 0);
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                    server.Recycle(incoming);
                }
            }
        }

    }
}

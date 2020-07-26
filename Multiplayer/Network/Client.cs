using Lidgren.Network;
using Multiplayer.Network.Packet;
using Multiplayer.Network.Packet.World;
using Placemaker;
using Placemaker.Quads;
using System;
using System.Collections.Generic;
using System.Threading;
using TSML.Event;
using Unity.Mathematics;

namespace Multiplayer.Network
{
    public class Client
    {
        public List<string> Output = new List<string>();

        private NetClient client;

        private void Print(string message)
        {
            Output.Insert(0, message);
        }

        public void Init(string name, string ip, int port)
        {
            Print($"Starting client, connecting to {ip}:{port}");

            NetPeerConfiguration config = new NetPeerConfiguration(Multiplayer.PLUGIN_IDENTIFIER)
            {
                AutoFlushSendQueue = false
            };

            client = new NetClient(config);
            client.Start();

            client.RegisterReceivedCallback(new SendOrPostCallback(Update));

            NetOutgoingMessage outgoing = client.CreateMessage(name);
            client.Connect(ip, port, outgoing);

            TSML.Event.EventHandler.Listeners += OnEvent;
        }

        private void OnEvent(Event e)
        {
            if (e is EventGroundClickerAddClick @event)
            {
                WorldMaster master = BootMaster.instance.worldMaster;
                GroundClicker gc = @event.GroundClicker;

                PacketAddVoxel packetAddVoxel = new PacketAddVoxel(client)
                {
                    ValidAdd = master.hoverData.validAdd,
                    DestinationHeight = master.hoverData.dstHeight,
                    VertAngle = master.hoverData.dstVert.angle,
                    HexPos = master.hoverData.dstVert.hexPos,
                    VoxelType = gc.currentVoxelType,
                    PlanePos = master.hoverData.dstVert.planePos
                };
                client.SendMessage(packetAddVoxel.Serialize(), NetDeliveryMethod.ReliableOrdered);
                client.FlushSendQueue(); 
            }
        }

        public bool IsRunning()
        {
            return client != null && client.Status != NetPeerStatus.NotRunning;
        }

        public void Shutdown(string reason = "Client disconnecting")
        {
            client?.Shutdown(reason);
        }

        public void Update(object peer)
        {
            NetIncomingMessage incoming;
            while ((incoming = client.ReadMessage()) != null)
            {
                switch (incoming.MessageType)
                {
                    case NetIncomingMessageType.VerboseDebugMessage:
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.ErrorMessage:
                        {
                            string message = incoming.ReadString();
                            Print(message);
                        }
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        {
                            NetConnectionStatus status = (NetConnectionStatus)incoming.ReadByte();
                            string reason = incoming.ReadString();
                            
                            switch (status)
                            {
                                case NetConnectionStatus.Connected:
                                    {
                                        Print($"{status}: {reason}");
                                    }
                                    break;
                                case NetConnectionStatus.Disconnected:
                                    {
                                        Print($"{status}: {reason}");
                                        Shutdown(reason);
                                    }
                                    break;
                            }
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        {
                            PacketType packetType = (PacketType)incoming.ReadByte();
                            switch (packetType)
                            {
                                case PacketType.WORLD_DATA:
                                    {
                                        PacketWorld packet = new PacketWorld(client);
                                        packet.Deserialize(incoming);

                                        if (BootMaster.instance.worldMaster)
                                            BootMaster.instance.worldMaster.SaveCurrentAndLoadAsNew(packet.WorldData);
                                    }
                                    break;

                                case PacketType.ADD_VOXEL:
                                    {
                                        PacketAddVoxel packet = new PacketAddVoxel(client);
                                        packet.Deserialize(incoming);

                                        WorldMaster master = BootMaster.instance.worldMaster;
                                        Maker maker = master.maker;
                                        GroundClicker groundClicker = master.groundClicker;

                                        if (packet.ValidAdd)
                                        {
                                            Voxel voxel = AddClick(master, maker, packet);
                                            groundClicker.lastAddedVoxel = voxel;
                                            ClickEffect(master, packet, true, voxel);
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                }
                client.Recycle(incoming);
            }
        }

        private Voxel AddClick(WorldMaster master, Maker maker, PacketAddVoxel packet)
        {
            int dstHeight = packet.DestinationHeight;
            VoxelType voxelType = packet.VoxelType;
            Vert vert = new Vert()
            {
                angle = packet.VertAngle,
                hexPos = packet.HexPos
            };
            if (!vert.full)
                vert = master.grid.GetVertOrIterate(packet.HexPos, null);

            if (!vert.full || !master.graph.IsCoordinateAllowed(vert.hexPos))
                return null;

            maker.BeginNewAction();
            if (dstHeight == 0)
                voxelType = VoxelType.Ground;

            Voxel result = master.graph.AddVoxel(vert.hexPos, (byte)dstHeight, voxelType, true);
            maker.AddAction(vert.hexPos, (byte)dstHeight, VoxelType.Empty, voxelType);
            maker.EndAction();
            return result;
        }

        private void ClickEffect(WorldMaster master, PacketAddVoxel packet, bool add, Voxel voxel)
        {
            // todo: add source stuff for packet for remove, or make this method only on add
            // and add a new method for remove
            float2 v = add ? packet.PlanePos : packet.PlanePos;
            int height = add ? packet.DestinationHeight : packet.DestinationHeight;
            master.clickEffect.Click(add, v, height, voxel.type);
        }
    }
}

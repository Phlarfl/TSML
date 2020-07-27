using Lidgren.Network;
using Multiplayer.Network.Packet;
using Multiplayer.Network.Packet.World;
using Placemaker;
using Placemaker.Quads;
using System.Collections.Generic;
using System.Threading;
using TSML.Event;

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

            var config = new NetPeerConfiguration(Multiplayer.PLUGIN_IDENTIFIER)
            {
                AutoFlushSendQueue = false
            };

            client = new NetClient(config);
            client.Start();

            client.RegisterReceivedCallback(new SendOrPostCallback(Update));

            var outgoing = client.CreateMessage(name);
            client.Connect(ip, port, outgoing);

            TSML.Event.EventHandler.Listeners += OnEvent;
        }

        private void OnEvent(Event e)
        {
            var master = BootMaster.instance.worldMaster;
            if (e is EventGroundClickerAddClick eventAddClick)
            {
                var gc = eventAddClick.GroundClicker;

                var packetAddVoxel = new PacketAddVoxel(client)
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
            } else if (e is EventGroundClickerRemoveClick eventRemoveClick)
            {
                var gc = eventRemoveClick.GroundClicker;

                var packetRemoveVoxel = new PacketRemoveVoxel(client)
                {
                    ValidRemove = master.hoverData.validRemove,
                    DestinationHeight = master.hoverData.dstHeight,
                    HexPos = master.hoverData.dstVert.hexPos,
                    PlanePos = master.hoverData.dstVert.planePos,
                    VoxelPosition = master.hoverData.voxel.transform.position
                };
                client.SendMessage(packetRemoveVoxel.Serialize(), NetDeliveryMethod.ReliableOrdered);
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
                            var packetType = (PacketType)incoming.ReadByte();
                            switch (packetType)
                            {
                                case PacketType.WORLD_DATA:
                                    {
                                        var packet = new PacketWorld(client);
                                        packet.Deserialize(incoming);

                                        if (BootMaster.instance.worldMaster)
                                            BootMaster.instance.worldMaster.SaveCurrentAndLoadAsNew(packet.WorldData);
                                    }
                                    break;

                                case PacketType.ADD_VOXEL:
                                    {
                                        var packet = new PacketAddVoxel(client);
                                        packet.Deserialize(incoming);

                                        var master = BootMaster.instance.worldMaster;
                                        var maker = master.maker;
                                        var groundClicker = master.groundClicker;

                                        if (packet.ValidAdd)
                                        {
                                            var voxel = AddClick(master, maker, packet);
                                            groundClicker.lastAddedVoxel = voxel;
                                            master.clickEffect.Click(true, packet.PlanePos, packet.DestinationHeight, voxel.type);
                                        }
                                    }
                                    break;

                                case PacketType.REMOVE_VOXEL:
                                    {
                                        var packet = new PacketRemoveVoxel(client);
                                        packet.Deserialize(incoming);

                                        var master = BootMaster.instance.worldMaster;
                                        var maker = master.maker;

                                        if (packet.ValidRemove)
                                        {
                                            var voxel = RemoveClick(master, maker, packet);
                                            master.clickEffect.Click(false, packet.PlanePos, packet.DestinationHeight, voxel.type);
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
            var voxelType = packet.VoxelType;
            var vert = new Vert()
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

            var result = master.graph.AddVoxel(vert.hexPos, (byte)dstHeight, voxelType, true);
            maker.AddAction(vert.hexPos, (byte)dstHeight, VoxelType.Empty, voxelType);
            maker.EndAction();
            return result;
        }

        private Voxel RemoveClick(WorldMaster master, Maker maker, PacketRemoveVoxel packet)
        {
            maker.BeginNewAction();
            Voxel result = null;
            foreach (var voxel in UnityEngine.Object.FindObjectsOfType<Voxel>())
                if (voxel.transform.position.Equals(packet.VoxelPosition))
                {
                    result = voxel;
                    break;
                }
            maker.AddAction(packet.HexPos, result.height, result.type, VoxelType.Empty);
            master.graph.RemoveVoxel(result);
            maker.EndAction();
            return result;
        }
    }
}

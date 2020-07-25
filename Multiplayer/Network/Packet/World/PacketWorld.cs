using Lidgren.Network;

namespace Multiplayer.Network.Packet.World
{
    public class PacketWorld : Packet
    {
        public string WorldData { get; set; }

        public PacketWorld(NetPeer peer) : base(peer) { }

        protected override PacketType GetPacketType()
        {
            return PacketType.WORLD_DATA;
        }

        public override NetOutgoingMessage Serialize()
        {
            NetOutgoingMessage @out = base.Serialize();

            @out.Write(WorldData);

            return @out;
        }

        public override void Deserialize(NetIncomingMessage @in)
        {
            WorldData = @in.ReadString();
        }
    }
}

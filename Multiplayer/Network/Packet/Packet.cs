using Lidgren.Network;

namespace Multiplayer.Network.Packet
{
    public abstract class Packet
    {
        private NetPeer peer;

        public Packet(NetPeer peer)
        {
            this.peer = peer;
        }

        protected abstract PacketType GetPacketType();

        public virtual NetOutgoingMessage Serialize()
        {
            NetOutgoingMessage @out = peer.CreateMessage();
            @out.Write((byte) GetPacketType());
            return @out;
        }

        public abstract void Deserialize(NetIncomingMessage @in);

    }
}

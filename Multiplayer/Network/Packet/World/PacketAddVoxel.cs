using Lidgren.Network;
using Placemaker;
using Placemaker.Quads;
using Unity.Mathematics;

namespace Multiplayer.Network.Packet.World
{
    public class PacketAddVoxel : Packet
    {
        public bool ValidAdd { get; set; }
        public int DestinationHeight { get; set; }
        public float VertAngle { get; set; }
        public int2 HexPos { get; set; }
        public VoxelType VoxelType { get; set; }
        public float2 PlanePos { get; set; }

        public PacketAddVoxel(NetPeer peer) : base(peer) { }

        protected override PacketType GetPacketType()
        {
            return PacketType.ADD_VOXEL;
        }

        public override NetOutgoingMessage Serialize()
        {
            NetOutgoingMessage @out = base.Serialize();

            @out.Write(ValidAdd);
            @out.Write(DestinationHeight);
            @out.Write(VertAngle);
            @out.Write(HexPos.x);
            @out.Write(HexPos.y);
            @out.Write((byte) VoxelType);
            @out.Write(PlanePos.x);
            @out.Write(PlanePos.y);

            return @out;
        }

        public override void Deserialize(NetIncomingMessage @in)
        {
            ValidAdd = @in.ReadBoolean();
            DestinationHeight = @in.ReadInt32();
            VertAngle = @in.ReadFloat();
            HexPos = new int2(@in.ReadInt32(), @in.ReadInt32());
            VoxelType = (VoxelType)@in.ReadByte();
            PlanePos = new float2(@in.ReadFloat(), @in.ReadFloat());
        }
    }
}

using Lidgren.Network;
using Placemaker;
using Unity.Mathematics;
using UnityEngine;

namespace Multiplayer.Network.Packet.World
{
    public class PacketRemoveVoxel : Packet
    {
        public bool ValidRemove { get; set; }
        public int DestinationHeight { get; set; }
        public int2 HexPos { get; set; }
        public float2 PlanePos { get; set; }
        public Vector3 VoxelPosition { get; set; }

        public PacketRemoveVoxel(NetPeer peer) : base(peer) { }

        protected override PacketType GetPacketType()
        {
            return PacketType.REMOVE_VOXEL;
        }

        public override NetOutgoingMessage Serialize()
        {
            NetOutgoingMessage @out = base.Serialize();

            @out.Write(ValidRemove);
            @out.Write(DestinationHeight);
            @out.Write(HexPos.x);
            @out.Write(HexPos.y);
            @out.Write(PlanePos.x);
            @out.Write(PlanePos.y);
            @out.Write(VoxelPosition.x);
            @out.Write(VoxelPosition.y);
            @out.Write(VoxelPosition.z);

            return @out;
        }

        public override void Deserialize(NetIncomingMessage @in)
        {
            ValidRemove = @in.ReadBoolean();
            DestinationHeight = @in.ReadInt32();
            HexPos = new int2(@in.ReadInt32(), @in.ReadInt32());
            PlanePos = new float2(@in.ReadFloat(), @in.ReadFloat());
            VoxelPosition = new Vector3(@in.ReadFloat(), @in.ReadFloat(), @in.ReadFloat());
        }
    }
}

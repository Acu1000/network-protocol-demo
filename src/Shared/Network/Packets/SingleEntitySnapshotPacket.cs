using System;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Network.Packets;

public readonly record struct SingleEntitySnapshotPacket : IPacket<SingleEntitySnapshotPacket>
{
    public const int PacketMinSize = 17;
    
    public readonly PacketType PacketType = PacketType.SingleEntitySnapshot;

    public readonly UInt32 SnapshotId;
    public readonly UInt64 EntityId;
    public readonly EntityType EntityType;
    public readonly UInt16 EntityNetworkOwnerId;
    public readonly byte[] StateBytes;

    public SingleEntitySnapshotPacket(UInt32 snapshotId, UInt64 entityId, EntityType entityType, 
        UInt16 networkOwnerId, byte[] newStateBytes)
    {
        SnapshotId = snapshotId;
        EntityId = entityId;
        EntityType = entityType;
        EntityNetworkOwnerId = networkOwnerId;
        StateBytes = newStateBytes;
    }

    public SingleEntitySnapshotPacket()
    {
    }
    
    public static bool TryParse(ReadOnlySpan<byte> data, out SingleEntitySnapshotPacket packet)
    {
        if (data.Length < PacketMinSize)
        {
            packet = new SingleEntitySnapshotPacket();
            return false;
        }

        UInt32 snapshotId = BitConverter.ToUInt32(data.Slice(1, 4));
        UInt64 entityId = BitConverter.ToUInt64(data.Slice(5, 8));
        UInt16 entityTypeNum = BitConverter.ToUInt16(data.Slice(13, 2));
        UInt16 networkOwnerId = BitConverter.ToUInt16(data.Slice(15, 2));
        
        if (entityTypeNum == 0 || !Enum.IsDefined(typeof(EntityType), entityTypeNum))
        {
            packet = new SingleEntitySnapshotPacket();
            return false;
        }
        
        byte[] newState = data.Slice(17).ToArray();
        
        packet = new SingleEntitySnapshotPacket(snapshotId, entityId, (EntityType)entityTypeNum, networkOwnerId,
            newState);
        
        return true;
    }

    public void WriteBytesTo(Span<byte> buffer)
    {
        if (buffer.Length < PacketMinSize + StateBytes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer));
        }
        
        buffer[0] = (byte)PacketType.SingleEntitySnapshot;
        BitConverter.TryWriteBytes(buffer.Slice(1, 4), SnapshotId);
        BitConverter.TryWriteBytes(buffer.Slice(5, 8), EntityId);
        BitConverter.TryWriteBytes(buffer.Slice(13, 2), (UInt16)EntityType);
        BitConverter.TryWriteBytes(buffer.Slice(15, 2), EntityNetworkOwnerId);
        StateBytes.CopyTo(buffer.Slice(17));
    }
    
    public byte[] ToBytes()
    {
        byte[] buffer = new byte[PacketMinSize + StateBytes.Length];
        WriteBytesTo(buffer);
        return buffer;
    }
}
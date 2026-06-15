using System;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Network.Packets;

public record struct SingleEntityUpdatePacket : IPacket<SingleEntityUpdatePacket>
{
    public const int PacketMinSize = 11;
    
    public readonly PacketType PacketType = PacketType.SingleEntityUpdate;
    
    public readonly UInt64 EntityID;
    public readonly EntityType EntityType;
    public readonly byte[] NewStateBytes;

    public SingleEntityUpdatePacket(UInt64 entityId, EntityType entityType, byte[] newStateBytes)
    {
        EntityID = entityId;
        EntityType = entityType;
        NewStateBytes = newStateBytes;
    }

    public SingleEntityUpdatePacket()
    {
    }
    
    public static bool TryParse(ReadOnlySpan<byte> data, out SingleEntityUpdatePacket packet)
    {
        if (data.Length < PacketMinSize)
        {
            packet = new SingleEntityUpdatePacket();
            return false;
        }

        UInt64 entityId = BitConverter.ToUInt64(data.Slice(1, 8));
        UInt16 entityTypeNum = BitConverter.ToUInt16(data.Slice(9, 2));
        
        if (entityTypeNum == 0 || !Enum.IsDefined(typeof(EntityType), entityTypeNum))
        {
            packet = new SingleEntityUpdatePacket();
            return false;
        }
        
        byte[] newState = data.Slice(11).ToArray();
        
        packet  = new SingleEntityUpdatePacket(entityId, (EntityType)entityTypeNum, newState);
        
        return true;
    }

    public void WriteBytesTo(Span<byte> buffer)
    {
        if (buffer.Length < PacketMinSize + NewStateBytes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer));
        }
        
        buffer[0] = (byte)PacketType.SingleEntityUpdate;
        BitConverter.TryWriteBytes(buffer.Slice(1, 8), EntityID);
        BitConverter.TryWriteBytes(buffer.Slice(9, 2), (UInt16)EntityType);
        NewStateBytes.CopyTo(buffer.Slice(11));
    }
    
    public byte[] ToBytes()
    {
        byte[] buffer = new byte[PacketMinSize + NewStateBytes.Length];
        WriteBytesTo(buffer);
        return buffer;
    }
}
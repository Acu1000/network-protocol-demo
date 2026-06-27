using System;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Network.Packets;

public readonly record struct SingleEntityCreatePacket : IPacket<SingleEntityCreatePacket>
{
    public const int PacketMinSize = 11;
    
    public readonly PacketType PacketType = PacketType.SingleEntityCreate;
    
    public readonly UInt64 EntityId;
    public readonly EntityType EntityType;
    public readonly byte[] InitialStateBytes;

    public SingleEntityCreatePacket(UInt64 entityId, EntityType entityType, byte[] newStateBytes)
    {
        EntityId = entityId;
        EntityType = entityType;
        InitialStateBytes = newStateBytes;
    }

    public SingleEntityCreatePacket()
    {
    }
    
    public static bool TryParse(ReadOnlySpan<byte> data, out SingleEntityCreatePacket packet)
    {
        if (data.Length < PacketMinSize)
        {
            packet = new SingleEntityCreatePacket();
            return false;
        }

        UInt64 entityId = BitConverter.ToUInt64(data.Slice(1, 8));
        UInt16 entityTypeNum = BitConverter.ToUInt16(data.Slice(9, 2));
        
        if (entityTypeNum == 0 || !Enum.IsDefined(typeof(EntityType), entityTypeNum))
        {
            packet = new SingleEntityCreatePacket();
            return false;
        }
        
        byte[] newState = data.Slice(11).ToArray();
        
        packet  = new SingleEntityCreatePacket(entityId, (EntityType)entityTypeNum, newState);
        
        return true;
    }

    public void WriteBytesTo(Span<byte> buffer)
    {
        if (buffer.Length < PacketMinSize + InitialStateBytes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer));
        }
        
        buffer[0] = (byte)PacketType.SingleEntityCreate;
        BitConverter.TryWriteBytes(buffer.Slice(1, 8), EntityId);
        BitConverter.TryWriteBytes(buffer.Slice(9, 2), (UInt16)EntityType);
        InitialStateBytes.CopyTo(buffer.Slice(11));
    }
    
    public byte[] ToBytes()
    {
        byte[] buffer = new byte[PacketMinSize + InitialStateBytes.Length];
        WriteBytesTo(buffer);
        return buffer;
    }
}
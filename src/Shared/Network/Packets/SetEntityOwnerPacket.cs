using System;

namespace Protocol.Shared.Network.Packets;

public readonly record struct SetEntityOwnerPacket : IPacket<SetEntityOwnerPacket>
{
    public const int PacketMinSize = 11;

    public readonly PacketType PacketType = PacketType.SetEntityOwner;
    
    public readonly UInt64 EntityId;
    public readonly UInt16 NewOwnerId;

    public SetEntityOwnerPacket(UInt64 entityId, UInt16 newOwnerId)
    {
        EntityId = entityId;
        NewOwnerId = newOwnerId;
    }
    
    public SetEntityOwnerPacket()
    {
    }
    
    public static bool TryParse(ReadOnlySpan<byte> data, out SetEntityOwnerPacket packet)
    {
        if (data.Length < PacketMinSize)
        {
            packet = default;
            return false;
        }

        UInt64 entityId = BitConverter.ToUInt64(data.Slice(1, 8));
        UInt16 newOwnerId = BitConverter.ToUInt16(data.Slice(9, 2));
        
        packet = new SetEntityOwnerPacket(entityId, newOwnerId);
        return true;
    }

    public void WriteBytesTo(Span<byte> buffer)
    {
        if (buffer.Length < PacketMinSize)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer));
        }

        buffer[0] = (byte)PacketType.SetEntityOwner;
        BitConverter.TryWriteBytes(buffer.Slice(1, 8), EntityId);
        BitConverter.TryWriteBytes(buffer.Slice(9, 2), NewOwnerId);
    }

    public byte[] ToBytes()
    {
        byte[] buffer = new byte[PacketMinSize];
        WriteBytesTo(buffer);
        return buffer;
    }
}
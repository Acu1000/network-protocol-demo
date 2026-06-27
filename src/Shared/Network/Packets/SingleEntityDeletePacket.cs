using System;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Network.Packets;

public readonly record struct SingleEntityDeletePacket : IPacket<SingleEntityDeletePacket>
{
    public const int PacketMinSize = 9;
    
    public readonly PacketType PacketType = PacketType.SingleEntityDelete;
    
    public readonly UInt64 EntityId;

    public SingleEntityDeletePacket(UInt64 entityId)
    {
        EntityId = entityId;
    }

    public SingleEntityDeletePacket()
    {
    }
    
    public static bool TryParse(ReadOnlySpan<byte> data, out SingleEntityDeletePacket packet)
    {
        if (data.Length < PacketMinSize)
        {
            packet = default;
            return false;
        }
        
        packet = new SingleEntityDeletePacket(BitConverter.ToUInt64(data.Slice(1, 8)));
        return true;
    }

    public void WriteBytesTo(Span<byte> buffer)
    {
        if (buffer.Length < PacketMinSize)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer));
        }
        
        buffer[0] = (byte)PacketType.SingleEntityDelete;
        BitConverter.TryWriteBytes(buffer.Slice(1, 8), EntityId);
    }

    public byte[] ToBytes()
    {
        byte[] buffer = new byte[PacketMinSize];
        WriteBytesTo(buffer);
        return buffer;
    }
}
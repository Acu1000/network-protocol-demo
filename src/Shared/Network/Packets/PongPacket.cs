using System;
using System.Runtime.InteropServices;

namespace Protocol.Shared.Network.Packets;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct PongPacket : IPacket<PongPacket>
{
    public const int PacketMinSize = 1;
    
    public readonly PacketType PacketType = PacketType.Pong;
    
    public PongPacket()
    {
    }
    
    public static bool TryParse(ReadOnlySpan<byte> data, out PongPacket packet)
    {
        throw new NotImplementedException();
    }

    public void WriteBytesTo(Span<byte> buffer)
    {
        if (buffer.Length < PacketMinSize)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer));
        }
        
        buffer[0] = (byte)PacketType.Pong;
    }
    
    public byte[] ToBytes()
    {
        byte[] data = new byte[PacketMinSize];
        WriteBytesTo(data);
        return data;
    }
}
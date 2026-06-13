using System;

namespace Protocol.Shared.Network.Packets;

public readonly record struct ConnectRequestPacket
{
    public const int PacketMinSize = 9;
    
    public readonly PacketType PacketType = PacketType.ConnectRequest;

    public readonly UInt64 LoginToken;

    public ConnectRequestPacket(UInt64 loginToken)
    {
        LoginToken = loginToken;
    }
    
    public ConnectRequestPacket()
    {
    }

    public static bool TryParse(ReadOnlySpan<byte> data, out ConnectRequestPacket packet)
    {
        if (data.Length < PacketMinSize)
        {
            packet = new ConnectRequestPacket();
            return false;
        }

        packet = new ConnectRequestPacket(BitConverter.ToUInt64(data.Slice(1, 8)));
        
        return true;
    }

    public void WriteBytesTo(Span<byte> buffer)
    {
        if (buffer.Length < PacketMinSize)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer));
        }
        
        buffer[0] = (byte)PacketType.ConnectRequest;
        
        BitConverter.TryWriteBytes(buffer.Slice(1, 8), LoginToken);
    }
    
    public byte[] ToBytes()
    {
        byte[] data = new byte[PacketMinSize];
        WriteBytesTo(data);
        return data;
    }
}
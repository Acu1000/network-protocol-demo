using System;

namespace Protocol.Shared.Network.Packets;

public readonly record struct ConnectAcceptPacket : IPacket<ConnectAcceptPacket>
{
    public const int PacketMinSize = 3;

    public readonly PacketType PacketType = PacketType.ConnectAccept;

    public readonly UInt16 ClientId;

    public ConnectAcceptPacket(UInt16 clientId)
    {
        ClientId = clientId;
    }

    public ConnectAcceptPacket()
    {
    }

    public static bool TryParse(ReadOnlySpan<byte> data, out ConnectAcceptPacket packet)
    {
        if (data.Length < PacketMinSize)
        {
            packet = new ConnectAcceptPacket();
            return false;
        }

        if ((PacketType)data[0] != PacketType.ConnectAccept)
        {
            packet = new ConnectAcceptPacket();
            return false;
        }

        packet = new ConnectAcceptPacket(BitConverter.ToUInt16(data.Slice(1, 2)));

        return true;
    }

    public void WriteBytesTo(Span<byte> buffer)
    {
        if (buffer.Length < PacketMinSize)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer));
        }

        buffer[0] = (byte)PacketType.ConnectAccept;

        BitConverter.TryWriteBytes(buffer.Slice(1, 2), ClientId);
    }

    public byte[] ToBytes()
    {
        byte[] data = new byte[PacketMinSize];
        WriteBytesTo(data);
        return data;
    }
}
using System;

namespace Protocol.Shared.Network.Packets;

public readonly record struct DisconnectAcceptPacket : IPacket<DisconnectAcceptPacket>
{
    public const int PacketMinSize = 3;

    public readonly PacketType PacketType = PacketType.DisconnectAccept;

    public readonly UInt16 ClientId;

    public DisconnectAcceptPacket(UInt16 clientId)
    {
        ClientId = clientId;
    }

    public DisconnectAcceptPacket()
    {
    }

    public static bool TryParse(ReadOnlySpan<byte> data, out DisconnectAcceptPacket packet)
    {
        if (data.Length < PacketMinSize)
        {
            packet = new DisconnectAcceptPacket();
            return false;
        }

        if ((PacketType)data[0] != PacketType.DisconnectAccept)
        {
            packet = new DisconnectAcceptPacket();
            return false;
        }

        packet = new DisconnectAcceptPacket(BitConverter.ToUInt16(data.Slice(1, 2)));

        return true;
    }

    public void WriteBytesTo(Span<byte> buffer)
    {
        if (buffer.Length < PacketMinSize)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer));
        }

        buffer[0] = (byte)PacketType.DisconnectAccept;

        BitConverter.TryWriteBytes(buffer.Slice(1, 2), ClientId);
    }

    public byte[] ToBytes()
    {
        byte[] data = new byte[PacketMinSize];
        WriteBytesTo(data);
        return data;
    }
}
using System;

namespace Protocol.Shared.Network.Packets;

public readonly record struct DisconnectRequestPacket : IPacket<DisconnectRequestPacket>
{
    public const int PacketMinSize = 3;

    public readonly PacketType PacketType = PacketType.DisconnectRequest;

    public readonly UInt16 ClientId;

    public DisconnectRequestPacket(UInt16 clientId)
    {
        ClientId = clientId;
    }

    public DisconnectRequestPacket()
    {
    }

    public static bool TryParse(ReadOnlySpan<byte> data, out DisconnectRequestPacket packet)
    {
        if (data.Length < PacketMinSize)
        {
            packet = new DisconnectRequestPacket();
            return false;
        }

        if ((PacketType)data[0] != PacketType.DisconnectRequest)
        {
            packet = new DisconnectRequestPacket();
            return false;
        }

        packet = new DisconnectRequestPacket(BitConverter.ToUInt16(data.Slice(1, 2)));

        return true;
    }

    public void WriteBytesTo(Span<byte> buffer)
    {
        if (buffer.Length < PacketMinSize)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer));
        }

        buffer[0] = (byte)PacketType.DisconnectRequest;

        BitConverter.TryWriteBytes(buffer.Slice(1, 2), ClientId);
    }

    public byte[] ToBytes()
    {
        byte[] data = new byte[PacketMinSize];
        WriteBytesTo(data);
        return data;
    }
}
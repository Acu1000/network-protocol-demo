using System;

namespace Protocol.Shared.Network.Packets;

public readonly record struct ConnectAcceptPacket : IPacket<ConnectAcceptPacket>
{
    public const int PacketMinSize = 7;

    public readonly PacketType PacketType = PacketType.ConnectAccept;

    public readonly UInt16 ClientId;
    public readonly UInt32 SessionToken;

    public ConnectAcceptPacket(UInt16 clientId, UInt32 sessionToken)
    {
        ClientId = clientId;
        SessionToken = sessionToken;
    }

    public ConnectAcceptPacket()
    {
    }

    public static bool TryParse(ReadOnlySpan<byte> data, out ConnectAcceptPacket packet)
    {
        if (data.Length < PacketMinSize)
        {
            packet = default;
            return false;
        }

        packet = new ConnectAcceptPacket(
            BitConverter.ToUInt16(data.Slice(1, 2)),
            BitConverter.ToUInt32(data.Slice(3, 4))
            );

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
        BitConverter.TryWriteBytes(buffer.Slice(3, 4), SessionToken);
    }

    public byte[] ToBytes()
    {
        byte[] data = new byte[PacketMinSize];
        WriteBytesTo(data);
        return data;
    }
}
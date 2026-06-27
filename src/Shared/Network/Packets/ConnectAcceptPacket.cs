using System;
using Protocol.Shared.Models;

namespace Protocol.Shared.Network.Packets;

public readonly record struct ConnectAcceptPacket : IPacket<ConnectAcceptPacket>
{
    public const int PacketMinSize = 1 + 2 + 32;

    public readonly PacketType PacketType = PacketType.ConnectAccept;

    public readonly UInt16 ClientId;
    public readonly SessionToken SessionToken; // TEMPORARY SOLUTION, TOKEN EXCHANGE SHOULD HAPPEN OVER SECURE CHANNEL

    public ConnectAcceptPacket(UInt16 clientId, SessionToken sessionToken)
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

        SessionToken sessionToken = default;
        data.Slice(3, 32).CopyTo(sessionToken);
        
        packet = new ConnectAcceptPacket(
            BitConverter.ToUInt16(data.Slice(1, 2)),
            sessionToken
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
        ((ReadOnlySpan<byte>)SessionToken).CopyTo(buffer.Slice(3, 32));
    }

    public byte[] ToBytes()
    {
        byte[] data = new byte[PacketMinSize];
        WriteBytesTo(data);
        return data;
    }
}
using System;
using System.Linq;

namespace Protocol.Shared.Network.Packets;

public readonly record struct ClientPacketHeader
{
    public static int HeaderSize = 4;
    
    public readonly UInt32 SessionToken;

    public ClientPacketHeader(UInt32 sessionToken)
    {
        SessionToken = sessionToken;
    }
    
    public static ClientPacketHeader Zero => new ClientPacketHeader(0);
    
    public static bool TryExtract(ReadOnlySpan<byte> data, out ClientPacketHeader header,
        out ReadOnlySpan<byte> remainingData)
    {
        if (data.Length < HeaderSize)
        {
            header = default;
            remainingData = default;
            return false;
        }

        header = new ClientPacketHeader(BitConverter.ToUInt32(data.Slice(0,4)));
        remainingData = data.Slice(HeaderSize);
        return true;
    }

    public byte[] ToBytes()
    {
        byte[] bytes = new byte[HeaderSize];
        BitConverter.TryWriteBytes(bytes, SessionToken);
        return bytes;
    }
    
    // TODO: optimize to get rid of array copy
    public byte[] AppendPacket(ReadOnlySpan<byte> data)
    {
        return [.. ToBytes(), .. data];
    }
}
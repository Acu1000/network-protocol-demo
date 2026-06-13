using System.Net;

namespace Protocol.Shared.Network.Packets;

public readonly record struct RawPacket
{
    public readonly EndPoint SourceEndPoint;
    public readonly byte[] PoolBuffer;
    public readonly int NumBytes;
    
    public RawPacket(EndPoint sourceEndPoint, byte[] poolBuffer, int numBytes)
    {
        SourceEndPoint = sourceEndPoint;
        PoolBuffer = poolBuffer;
        NumBytes = numBytes;
    }
}
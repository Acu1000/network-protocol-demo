using System;
using System.Buffers;
using System.Net;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Server.Network;

public class ServerUdpHandler : BaseUdpHandler
{
    private readonly IPEndPoint _localEndPoint;
	
    public ServerUdpHandler(int serverPort)
    {
        _localEndPoint = new IPEndPoint(IPAddress.Any, serverPort);
    }
    
    public void RoutePackets(ServerPacketRouter router)
    {
        while (Reader.TryRead(out RawPacket rawPacket))
        {
            try
            {
                ReadOnlySpan<byte> packetSpan = rawPacket.PoolBuffer.AsSpan(0, rawPacket.NumBytes);
                router.Route(packetSpan, rawPacket.SourceEndPoint);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rawPacket.PoolBuffer);
            }
        }
    }

    public void StartListening()
    {
        StartListening(_localEndPoint);
    }
}
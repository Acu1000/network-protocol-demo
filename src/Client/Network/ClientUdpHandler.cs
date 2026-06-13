using System;
using System.Buffers;
using System.Net;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client.Network;

public class ClientUdpHandler : BaseUdpHandler
{
    private readonly IPEndPoint _serverEndPoint;
    private readonly IPEndPoint _localEndPoint;
    
    public ClientUdpHandler(string serverAddress, int serverPort, int localPort) : base()
    {
        _serverEndPoint = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);
        _localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
    }

    public void SendToServer(ReadOnlyMemory<byte> data)
    {
        Send(data, _serverEndPoint);
    }
    
    public void RoutePackets(PacketRouter router)
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
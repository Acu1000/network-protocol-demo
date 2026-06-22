using System;
using System.Net;
using System.Threading.Tasks;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client.Network;

public class MockClientSessionManager : IClientSessionManager
{
    private readonly UdpHandler _udpHandler;

    public MockClientSessionManager(UdpHandler udpHandler, int localPort)
    {
        _udpHandler = udpHandler;
    }

    public ushort ClientId { get; }

    public Task<bool> TryConnectToServer(IPEndPoint endPoint)
    {
        throw new NotImplementedException();
    }

    public void DisconnectFromServer()
    {
        throw new NotImplementedException();
    }

    public void SendToServer<T>(IPacket<T> packet) where T : IPacket<T>
    {
        _udpHandler.Send(packet.ToBytes(), new IPEndPoint(IPAddress.Loopback, 12345));
    }

    public void HandleConnectAcceptPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        throw new NotImplementedException();
    }

    public void HandleDisconnectAcceptPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        throw new NotImplementedException();
    }

    public void HandlePingPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        throw new NotImplementedException();
    }

    public void HandlePongPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        throw new NotImplementedException();
    }

    public bool IsServerEndpoint(IPEndPoint endpoint)
    {
        throw new NotImplementedException();
    }
}
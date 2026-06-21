using System;
using System.Net;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Server.Network;

public class MockServerSessionManager : IServerSessionManager
{
    private readonly UdpHandler _udpHandler;

    public MockServerSessionManager(UdpHandler udpHandler)
    {
        _udpHandler = udpHandler;
    }
    
    public void SendToClient<T>(IPacket<T> packet, ushort clientId) where T : IPacket<T>
    {
        int targetPort;

        if (clientId == 1)
            targetPort = 54321;
        else if (clientId == 2)
            targetPort = 54322;
        else
            return;
        
        _udpHandler.Send(packet.ToBytes(), new IPEndPoint(IPAddress.Loopback, targetPort));
    }

    public void SendToAllClients<T>(IPacket<T> packet) where T : IPacket<T>
    {
        _udpHandler.Send(packet.ToBytes(), new IPEndPoint(IPAddress.Loopback, 54321));
        _udpHandler.Send(packet.ToBytes(), new IPEndPoint(IPAddress.Loopback, 54322));
    }

    public void SendToAllClientsExcept<T>(IPacket<T> packet, ushort skippedClientId) where T : IPacket<T>
    {
        if (skippedClientId != 1) 
            _udpHandler.Send(packet.ToBytes(), new IPEndPoint(IPAddress.Loopback, 54321));
        
        if (skippedClientId != 2) 
            _udpHandler.Send(packet.ToBytes(), new IPEndPoint(IPAddress.Loopback, 54322));
    }

    public void DisconnectClient(ushort clientId)
    {
        throw new NotImplementedException();
    }

    public void HandleConnectRequestPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
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

    public bool TryGetClientId(IPEndPoint sourceEndPoint, out ushort clientId)
    {
        throw new NotImplementedException();
    }
}
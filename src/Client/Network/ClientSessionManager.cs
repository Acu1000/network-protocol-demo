using System;
using System.Diagnostics;
using System.Net;
using Godot;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client.Network;

public class ClientSessionManager
{
    private readonly UdpHandler _udpHandler;
    
    private IPEndPoint _serverEndPoint = new(IPAddress.Loopback, 12345); // TODO: set when connecting

    public ClientSessionManager(UdpHandler udpHandler)
    {
        _udpHandler = udpHandler;
    }
    
    public void HandlePingPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        GD.Print("Client: Ping received");
        
        // Send Pong in response
        byte[] data = new PongPacket().ToBytes();
        
        _udpHandler.Send(data, _serverEndPoint);
    }
}
using System;
using System.Collections.Generic;
using System.Net;
using Godot;
using Protocol.Server.Network.Models;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Server.Network;

public class ServerSessionManager
{
    private readonly UdpHandler _serverUdpHandler;
    
    private readonly List<ClientInfo> _clients = new();

    public ServerSessionManager(UdpHandler serverUdpHandler)
    {
        _serverUdpHandler = serverUdpHandler;
    }
    
    private void HandleConnectRequest(UInt64 loginToken, EndPoint endPoint)
    {
        // TODO: validate

        ClientInfo client = new();
        client.Username = loginToken.ToString();
        client.SessionToken = Random.Shared.NextInt64(); // TODO: use cryptographically safe generation
        client.EndPoint = endPoint;
        
        _clients.Add(client);
        
        GD.Print($"Player {client.Username} connected");
    }

    public void HandleConnectRequestPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (ConnectRequestPacket.TryParse(packetData, out var packet))
        {
            HandleConnectRequest(packet.LoginToken, sourceEndPoint);
        }
    }

    public void HandlePingPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        GD.Print("SERVER: Ping received");
        
        // Send Pong in response
        byte[] data = new PongPacket().ToBytes();
        
        _serverUdpHandler.Send(data, sourceEndPoint);
    }
}
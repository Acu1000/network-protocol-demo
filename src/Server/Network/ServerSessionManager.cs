using System;
using System.Collections.Generic;
using System.Net;
using Godot;
using Protocol.Server.Network.Models;
using Protocol.Shared.Network.Packets;

namespace Protocol.Server.Network;

public class ServerSessionManager
{
    List<ClientInfo> _clients = new();

    private void HandleConnectRequest(UInt64 loginToken)
    {
        // TODO: validate

        ClientInfo client = new();
        client.Username = loginToken.ToString();
        client.SessionToken = Random.Shared.NextInt64(); // TODO: use cryptographically safe generation
        
        _clients.Add(client);
        
        GD.Print($"Player {client.Username} connected");
    }

    public void HandleConnectRequestPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (ConnectRequestPacket.TryParse(packetData, out var packet))
        {
            HandleConnectRequest(packet.LoginToken);
        }
    }
}
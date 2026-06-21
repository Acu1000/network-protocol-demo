using System;
using System.Collections.Generic;
using System.Net;
using Godot;
using Protocol.Server.Network.Models;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Server.Network;

public class ServerSessionManager : IServerSessionManager
{
    private readonly UdpHandler _serverUdpHandler;

    private readonly List<ClientInfo> _clients = new();

    private UInt16 _nextClientId = 1;

    public ServerSessionManager(UdpHandler serverUdpHandler)
    {
        _serverUdpHandler = serverUdpHandler;
    }

    public void SendToClient<T>(IPacket<T> packet, UInt16 clientId) where T : IPacket<T>
    {
        ClientInfo? client = _clients.Find(c => c.ClientId == clientId);

        if (client == null)
        {
            GD.PrintErr($"SERVER: Cannot send packet. Client {clientId} not found.");
            return;
        }

        _serverUdpHandler.Send(packet.ToBytes(), client.EndPoint);
    }

    public void SendToAllClients<T>(IPacket<T> packet) where T : IPacket<T>
    {
        byte[] data = packet.ToBytes();

        foreach (ClientInfo client in _clients)
        {
            _serverUdpHandler.Send(data, client.EndPoint);
        }
    }

    public void SendToAllClientsExcept<T>(IPacket<T> packet, UInt16 skippedClientId) where T : IPacket<T>
    {
        byte[] data = packet.ToBytes();

        foreach (ClientInfo client in _clients)
        {
            if (client.ClientId == skippedClientId)
            {
                continue;
            }

            _serverUdpHandler.Send(data, client.EndPoint);
        }
    }

    public void DisconnectClient(UInt16 clientId)
    {
        int removedCount = _clients.RemoveAll(c => c.ClientId == clientId);

        if (removedCount > 0)
        {
            GD.Print($"SERVER: Client {clientId} disconnected");
        }
        else
        {
            GD.PrintErr($"SERVER: Cannot disconnect. Client {clientId} not found.");
        }
    }

    private void HandleConnectRequest(UInt64 loginToken, EndPoint endPoint)
    {
        if (endPoint is not IPEndPoint ipEndPoint)
        {
            GD.PrintErr("SERVER: Connect request rejected. Invalid endpoint.");
            return;
        }

        if (TryGetClientId(ipEndPoint, out UInt16 existingClientId))
        {
            GD.Print($"SERVER: Client already connected. ClientId: {existingClientId}");
            return;
        }

        ClientInfo client = new()
        {
            ClientId = _nextClientId++,
            Username = loginToken.ToString(),
            EndPoint = endPoint
        };

        _clients.Add(client);

        GD.Print($"SERVER: Player {client.Username} connected with id {client.ClientId}");

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

        if (sourceEndPoint is not IPEndPoint ipEndPoint)
        {
            return;
        }

        if (!TryGetClientId(ipEndPoint, out UInt16 clientId))
        {
            GD.PrintErr("SERVER: Ping rejected. Unknown client endpoint.");
            return;
        }

        GD.Print($"SERVER: Ping from client {clientId}");

        PongPacket pongPacket = new();
        _serverUdpHandler.Send(pongPacket.ToBytes(), sourceEndPoint);
    }

    public void HandlePongPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (sourceEndPoint is not IPEndPoint ipEndPoint)
        {
            return;
        }

        if (!TryGetClientId(ipEndPoint, out UInt16 clientId))
        {
            GD.PrintErr("SERVER: Pong rejected. Unknown client endpoint.");
            return;
        }

        GD.Print($"SERVER: Pong received from client {clientId}");

    }

    public bool TryGetClientId(IPEndPoint sourceEndPoint, out UInt16 clientId)
    {
        foreach (ClientInfo client in _clients)
        {
            if (client.EndPoint is not IPEndPoint clientEndPoint)
            {
                continue;
            }

            if (clientEndPoint.Address.Equals(sourceEndPoint.Address) &&
                clientEndPoint.Port == sourceEndPoint.Port)
            {
                clientId = client.ClientId;
                return true;
            }
        }

        clientId = default;
        return false;
    }
}
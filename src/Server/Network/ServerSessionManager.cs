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

    public IReadOnlyList<ClientInfo> Clients => _clients;

    public void SendToClient<T>(IPacket<T> packet, UInt16 clientId) where T : IPacket<T>
    {
        ClientInfo? client = _clients.Find(c => c.ClientId == clientId);

        if (client == null)
        {
            GD.PrintErr($"SERVER: Client {clientId} not found");
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

        GD.Print($"SERVER: Sent {typeof(T).Name} to all clients. Count: {_clients.Count}");
    }

    public void SendToAllClientsExcept<T>(IPacket<T> packet, UInt16 skippedClientId) where T : IPacket<T>
    {
        byte[] data = packet.ToBytes();

        int sentCount = 0;

        foreach (ClientInfo client in _clients)
        {
            if (client.ClientId == skippedClientId)
            {
                continue;
            }

            _serverUdpHandler.Send(data, client.EndPoint);
            sentCount++;
        }

        GD.Print($"SERVER: Sent {typeof(T).Name} to all clients except {skippedClientId}. Count: {sentCount}");
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
            GD.PrintErr($"SERVER: Tried to disconnect unknown client {clientId}");
        }
    }

    private void HandleConnectRequest(UInt64 loginToken, EndPoint endPoint)
    {
        if (endPoint is not IPEndPoint ipEndPoint)
        {
            GD.PrintErr("SERVER: Connect rejected. Invalid endpoint.");
            return;
        }

        if (TryGetClientId(ipEndPoint, out UInt16 existingClientId))
        {
            GD.Print($"SERVER: Client already connected. ClientId: {existingClientId}");

            SendToClient(new ConnectAcceptPacket(existingClientId), existingClientId);
            return;
        }

        ClientInfo client = new()
        {
            ClientId = _nextClientId++,
            Username = loginToken.ToString(),
            EndPoint = endPoint
        };

        _clients.Add(client);

        GD.Print($"SERVER: Player {client.Username} connected as ClientId {client.ClientId}");

        SendToClient(new ConnectAcceptPacket(client.ClientId), client.ClientId);
    }

    public void HandleConnectRequestPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (!ConnectRequestPacket.TryParse(packetData, out ConnectRequestPacket packet))
        {
            GD.PrintErr("SERVER: Invalid ConnectRequestPacket");
            return;
        }

        HandleConnectRequest(packet.LoginToken, sourceEndPoint);
    }

    public void HandleDisconnectRequestPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (!DisconnectRequestPacket.TryParse(packetData, out DisconnectRequestPacket packet))
        {
            GD.PrintErr("SERVER: Invalid DisconnectRequestPacket");
            return;
        }

        if (sourceEndPoint is not IPEndPoint ipEndPoint)
        {
            return;
        }

        if (!TryGetClientId(ipEndPoint, out UInt16 endpointClientId))
        {
            GD.PrintErr("SERVER: Disconnect rejected. Unknown endpoint.");
            return;
        }

        if (endpointClientId != packet.ClientId)
        {
            GD.PrintErr($"SERVER: Disconnect rejected. Packet ClientId {packet.ClientId}, endpoint ClientId {endpointClientId}");
            return;
        }

        GD.Print($"SERVER: Disconnect request from ClientId {packet.ClientId}");

        _serverUdpHandler.Send(
            new DisconnectAcceptPacket(packet.ClientId).ToBytes(),
            sourceEndPoint
        );

        DisconnectClient(packet.ClientId);
    }

    public void HandlePingPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (sourceEndPoint is not IPEndPoint ipEndPoint)
        {
            return;
        }

        if (!TryGetClientId(ipEndPoint, out UInt16 clientId))
        {
            GD.PrintErr("SERVER: Ping rejected. Unknown client endpoint.");
            return;
        }

        GD.Print($"SERVER: Ping received from ClientId {clientId}");

        _serverUdpHandler.Send(new PongPacket().ToBytes(), sourceEndPoint);
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

        GD.Print($"SERVER: Pong received from ClientId {clientId}");
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
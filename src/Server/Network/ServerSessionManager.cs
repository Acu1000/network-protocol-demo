using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Godot;
using Protocol.Server.Network.Models;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Server.Network;

public class ServerSessionManager
{
    public event Action<ClientInfo> ClientConnected;
    
    private readonly UdpHandler _serverUdpHandler;
    private readonly PacketRouter _packetRouter;

    private readonly List<ClientInfo> _clients = new();

    private UInt16 _nextClientId = 1;

    public ServerSessionManager(UdpHandler serverUdpHandler, PacketRouter packetRouter)
    {
        _serverUdpHandler = serverUdpHandler;
        _packetRouter = packetRouter;
    }

    public IReadOnlyList<ClientInfo> Clients => _clients;
    
    public void SendToClient<T>(T packet, UInt16 clientId) where T : IPacket<T>
    {
        ClientInfo? client = _clients.Find(c => c.ClientId == clientId);

        if (client == null)
        {
            GD.PrintErr($"SERVER: Client {clientId} not found");
            return;
        }

        _serverUdpHandler.Send(packet.ToBytes(), client.EndPoint);
    }

    public void SendToAllClients<T>(T packet) where T : IPacket<T>
    {
        byte[] data = packet.ToBytes();

        foreach (ClientInfo client in _clients)
        {
            _serverUdpHandler.Send(data, client.EndPoint);
        }

        //GD.Print($"SERVER: Sent {typeof(T).Name} to all clients. Count: {_clients.Count}");
    }

    public void SendToAllClientsExcept<T>(T packet, UInt16 skippedClientId) where T : IPacket<T>
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

        //GD.Print($"SERVER: Sent {typeof(T).Name} to all clients except {skippedClientId}. Count: {sentCount}");
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
            
            // TODO: this whole part is unsafe and unoptimized
            ClientInfo clientInfo = Clients.First(info => info.EndPoint.Equals(ipEndPoint));
            
            SendToClient(new ConnectAcceptPacket(existingClientId, clientInfo.SessionToken), existingClientId);
            return;
        }

        // TODO: use cryptographically safe rng
        UInt32 sessionToken = (UInt32)Random.Shared.NextInt64(0, UInt32.MaxValue);

        ClientInfo client = new()
        {
            ClientId = _nextClientId++,
            Username = loginToken.ToString(),
            SessionToken = sessionToken,
            EndPoint = ipEndPoint
        };

        _clients.Add(client);
        ClientConnected.Invoke(client);

        GD.Print($"SERVER: Player {client.Username} connected as ClientId {client.ClientId}");

        SendToClient(new ConnectAcceptPacket(client.ClientId, sessionToken), client.ClientId);
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

    private ClientInfo? ValidateClientPacketHeader(ClientPacketHeader header, IPEndPoint sourceEndPoint)
    {
        ClientInfo? clientInfo = Clients.FirstOrDefault((info => info.SessionToken.Equals(header.SessionToken)));

        if (clientInfo is null)
        {
            return null;
        }

        if (!Equals(sourceEndPoint, clientInfo.EndPoint))
        {
            // TODO: Validate new endpoint and update ClientInfo (complicated)
            GD.Print("CLIENT ENDPOINT CHANGED");
        }
        
        return clientInfo;
    }
    
    public void Process()
    {
        while (_serverUdpHandler.TryGetPacket(out byte[] packetWithHeader, out EndPoint? sourceEndPoint))
        {
            // TODO: handle ConnectRequestPacket separately - it does not have this header
            if (!ClientPacketHeader.TryExtract(packetWithHeader, out ClientPacketHeader packetHeader, 
                    out ReadOnlySpan<byte> packet))
            {
                continue;
            }

            if (packetHeader.Equals(ClientPacketHeader.Zero) && packet.Length >= 1 &&
                packet[0] == (byte)PacketType.ConnectRequest)
            {
                HandleConnectRequestPacket(packet, sourceEndPoint!);
                continue;
            }
                
            ClientInfo? clientInfo = ValidateClientPacketHeader(packetHeader, (IPEndPoint)sourceEndPoint!);
            
            if (clientInfo is null)
            {
                GD.Print("VALIDATION FAILED, PACKET DROPPED");
                continue;
            }
            
            _packetRouter.Route(packet, sourceEndPoint!);
        }
    }
}
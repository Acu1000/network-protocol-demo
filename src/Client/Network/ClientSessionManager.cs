using System;
using System.Net;
using System.Threading.Tasks;
using Godot;
using Protocol.Client.Network.Models;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client.Network;

public class ClientSessionManager
{
    private readonly UdpHandler _udpHandler;
    
    private TaskCompletionSource<bool>? _connectCompletionSource;
    
    private ClientSessionInfo? _sessionInfo = null;
    
    private IPEndPoint? _pendingServerEndPoint = null;

    public bool Connected => _sessionInfo is not null;
    public IPEndPoint? ServerEndPoint => _sessionInfo?.ServerEndPoint;

    public ClientSessionManager(UdpHandler udpHandler)
    {
        _udpHandler = udpHandler;
    }
    
    // TODO: nullable
    public UInt16 ClientId => (_sessionInfo?.ClientId).Value;
    
    public async Task<bool> TryConnectToServer(IPEndPoint endPoint)
    {
        if (Connected)
        {
            GD.Print($"CLIENT: Already connected as ClientId {ClientId}");
            return true;
        }

        _connectCompletionSource = new TaskCompletionSource<bool>();

        UInt64 loginToken = (UInt64)Random.Shared.NextInt64(1, long.MaxValue);

        ConnectRequestPacket packet = new(loginToken);
        ClientPacketHeader header = ClientPacketHeader.Zero;
        
        _udpHandler.Send(header.AppendPacket(packet.ToBytes()), endPoint);

        _pendingServerEndPoint = endPoint;
        
        GD.Print($"CLIENT: ConnectRequest sent to {endPoint}");

        Task completedTask = await Task.WhenAny(
            _connectCompletionSource.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        );

        if (completedTask != _connectCompletionSource.Task)
        {
            GD.PrintErr("CLIENT: Connect timeout");

            _connectCompletionSource = null;
            _sessionInfo = null;

            return false;
        }

        bool result = await _connectCompletionSource.Task;

        _connectCompletionSource = null;

        return result;
    }

    public void DisconnectFromServer()
    {
        if (!Connected)
        {
            GD.PrintErr("CLIENT: Cannot disconnect. Client is not connected.");
            return;
        }

        DisconnectRequestPacket packet = new(ClientId);

        SendToServer(packet);

        GD.Print($"CLIENT: DisconnectRequest sent. ClientId {ClientId}");
    }

    public void SendToServer<T>(T packet) where T : IPacket<T>
    {
        // TODO: ConnectRequest packets should be handled separately
        if (_sessionInfo is null)
        {
            GD.PrintErr("CLIENT: Cannot send packet. Not connected.");
            return;
        }

        ClientPacketHeader header = new ClientPacketHeader(_sessionInfo.SessionToken);
        
        byte[] data = header.AppendPacket(packet.ToBytes());
        
        _udpHandler.Send(data, _sessionInfo.ServerEndPoint);
    }

    public void HandleConnectAcceptPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (sourceEndPoint is not IPEndPoint ipEndPoint)
        {
            return;
        }

        if (_pendingServerEndPoint is null || !ipEndPoint.Equals(_pendingServerEndPoint))
        {
            GD.PrintErr("CLIENT: ConnectAccept rejected. Unknown endpoint.");
            return;
        }
        
        if (!ConnectAcceptPacket.TryParse(packetData, out ConnectAcceptPacket packet))
        {
            GD.PrintErr("CLIENT: Invalid ConnectAcceptPacket");
            return;
        }
        
        _pendingServerEndPoint = null;

        _sessionInfo = new ClientSessionInfo()
        {
            ClientId = packet.ClientId,
            SessionToken = packet.SessionToken,
            ServerEndPoint = (IPEndPoint)sourceEndPoint,
        };
        

        GD.Print($"CLIENT: Connected. ClientId {ClientId}");

        _connectCompletionSource?.TrySetResult(true);
    }

    public void HandleDisconnectAcceptPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (sourceEndPoint is not IPEndPoint ipEndPoint)
        {
            return;
        }

        if (!IsServerEndpoint(ipEndPoint))
        {
            GD.PrintErr("CLIENT: DisconnectAccept rejected. Unknown endpoint.");
            return;
        }

        if (!DisconnectAcceptPacket.TryParse(packetData, out DisconnectAcceptPacket packet))
        {
            GD.PrintErr("CLIENT: Invalid DisconnectAcceptPacket");
            return;
        }

        GD.Print($"CLIENT: Disconnect accepted for ClientId {packet.ClientId}");

        _sessionInfo = null;
        _connectCompletionSource = null;
    }

    public void HandlePingPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (sourceEndPoint is not IPEndPoint ipEndPoint)
        {
            return;
        }

        if (!IsServerEndpoint(ipEndPoint))
        {
            GD.PrintErr("CLIENT: Ping rejected. Unknown endpoint.");
            return;
        }

        GD.Print("CLIENT: Ping received from server");

        SendToServer(new PongPacket());
    }

    public void HandlePongPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (sourceEndPoint is not IPEndPoint ipEndPoint)
        {
            return;
        }

        if (!IsServerEndpoint(ipEndPoint))
        {
            GD.PrintErr("CLIENT: Pong rejected. Unknown endpoint.");
            return;
        }

        GD.Print("CLIENT: Pong received from server");
    }

    public bool IsServerEndpoint(IPEndPoint endPoint)
    {
        if (_sessionInfo is null)
        {
            return false;
        }

        return endPoint.Equals(_sessionInfo.ServerEndPoint);
    }
}
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Godot;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client.Network;

public class ClientSessionManager : IClientSessionManager
{
    private readonly UdpHandler _udpHandler;

    private IPEndPoint? _serverEndPoint;

    private TaskCompletionSource<bool>? _connectCompletionSource;

    private bool _connected;

    public ClientSessionManager(UdpHandler udpHandler)
    {
        _udpHandler = udpHandler;
    }

    public async Task<bool> TryConnectToServer(IPEndPoint endPoint)
    {
        if (_connected && IsServerEndpoint(endPoint))
        {
            return true;
        }

        _serverEndPoint = endPoint;
        _connected = false;

        _connectCompletionSource = new TaskCompletionSource<bool>();

        UInt64 loginToken = (UInt64)Random.Shared.NextInt64(1, long.MaxValue);

        ConnectRequestPacket connectRequestPacket = new(loginToken);
        _udpHandler.Send(connectRequestPacket.ToBytes(), _serverEndPoint);

        GD.Print($"CLIENT: Connect request sent to {_serverEndPoint}");

        Task completedTask = await Task.WhenAny(
            _connectCompletionSource.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        );

        if (completedTask != _connectCompletionSource.Task)
        {
            GD.PrintErr("CLIENT: Connection timeout");

            _connectCompletionSource = null;
            _serverEndPoint = null;
            _connected = false;

            return false;
        }

        bool result = await _connectCompletionSource.Task;

        _connectCompletionSource = null;
        _connected = result;

        return result;
    }

    public void DisconnectFromServer()
    {
        if (!_connected)
        {
            return;
        }

        GD.Print("CLIENT: Disconnected from server");

        _connected = false;
        _serverEndPoint = null;
        _connectCompletionSource = null;

    }

    public void SendToServer<T>(IPacket<T> packet) where T : IPacket<T>
    {
        if (!_connected || _serverEndPoint == null)
        {
            GD.PrintErr("CLIENT: Cannot send packet. Client is not connected to server.");
            return;
        }

        _udpHandler.Send(packet.ToBytes(), _serverEndPoint);
    }

    public void HandleConnectAcceptPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (sourceEndPoint is not IPEndPoint ipEndPoint)
        {
            return;
        }

        if (!IsServerEndpoint(ipEndPoint))
        {
            GD.PrintErr("CLIENT: Connect accept rejected. Packet came from unknown endpoint.");
            return;
        }

        GD.Print("CLIENT: Connect accepted by server");

        _connected = true;
        _connectCompletionSource?.TrySetResult(true);
    }

    public void HandlePingPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        GD.Print("CLIENT: Ping received");

        if (sourceEndPoint is not IPEndPoint ipEndPoint)
        {
            return;
        }

        if (!IsServerEndpoint(ipEndPoint))
        {
            GD.PrintErr("CLIENT: Ping rejected. Packet came from unknown endpoint.");
            return;
        }

        PongPacket pongPacket = new();
        _udpHandler.Send(pongPacket.ToBytes(), sourceEndPoint);
    }

    public void HandlePongPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (sourceEndPoint is not IPEndPoint ipEndPoint)
        {
            return;
        }

        if (!IsServerEndpoint(ipEndPoint))
        {
            GD.PrintErr("CLIENT: Pong rejected. Packet came from unknown endpoint.");
            return;
        }

        GD.Print("CLIENT: Pong received from server");

    }

    public bool IsServerEndpoint(IPEndPoint endpoint)
    {
        if (_serverEndPoint == null)
        {
            return false;
        }

        return _serverEndPoint.Address.Equals(endpoint.Address) &&
               _serverEndPoint.Port == endpoint.Port;
    }
}
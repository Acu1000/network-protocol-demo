using System;
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
    
    private UInt16 _clientId;

    private bool _connected;

    public ClientSessionManager(UdpHandler udpHandler)
    {
        _udpHandler = udpHandler;
    }

    public bool IsConnected => _connected;

    // TODO: nullable
    public UInt16 ClientId => _clientId;

    public async Task<bool> TryConnectToServer(IPEndPoint endPoint)
    {
        if (_connected)
        {
            GD.Print($"CLIENT: Already connected as ClientId {_clientId}");
            return true;
        }

        _serverEndPoint = endPoint;
        _connectCompletionSource = new TaskCompletionSource<bool>();

        UInt64 loginToken = (UInt64)Random.Shared.NextInt64(1, long.MaxValue);

        ConnectRequestPacket packet = new(loginToken);

        _udpHandler.Send(packet.ToBytes(), _serverEndPoint);

        GD.Print($"CLIENT: ConnectRequest sent to {_serverEndPoint}");

        Task completedTask = await Task.WhenAny(
            _connectCompletionSource.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        );

        if (completedTask != _connectCompletionSource.Task)
        {
            GD.PrintErr("CLIENT: Connect timeout");

            _connectCompletionSource = null;
            _serverEndPoint = null;
            _connected = false;
            _clientId = 0;

            return false;
        }

        bool result = await _connectCompletionSource.Task;

        _connectCompletionSource = null;

        return result;
    }

    public void DisconnectFromServer()
    {
        if (!_connected || _serverEndPoint == null)
        {
            GD.PrintErr("CLIENT: Cannot disconnect. Client is not connected.");
            return;
        }

        DisconnectRequestPacket packet = new(_clientId);

        _udpHandler.Send(packet.ToBytes(), _serverEndPoint);

        GD.Print($"CLIENT: DisconnectRequest sent. ClientId {_clientId}");
    }

    public void SendToServer<T>(IPacket<T> packet) where T : IPacket<T>
    {
        if (!_connected || _serverEndPoint == null)
        {
            GD.PrintErr("CLIENT: Cannot send packet. Not connected.");
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
            GD.PrintErr("CLIENT: ConnectAccept rejected. Unknown endpoint.");
            return;
        }

        if (!ConnectAcceptPacket.TryParse(packetData, out ConnectAcceptPacket packet))
        {
            GD.PrintErr("CLIENT: Invalid ConnectAcceptPacket");
            return;
        }

        _clientId = packet.ClientId;
        _connected = true;

        GD.Print($"CLIENT: Connected. ClientId {_clientId}");

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

        _connected = false;
        _clientId = 0;
        _serverEndPoint = null;
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

        _udpHandler.Send(new PongPacket().ToBytes(), sourceEndPoint);
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
using System;
using System.Net;
using System.Threading.Tasks;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client.Network;

public interface IClientSessionManager
{
    public Task<bool> TryConnectToServer(IPEndPoint endPoint);
    public void DisconnectFromServer();
    public void SendToServer<T>(IPacket<T> packet) where T : IPacket<T>;

    public void HandleConnectAcceptPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint);
    public void HandleDisconnectAcceptPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint);
    public void HandlePingPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint);
    public void HandlePongPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint);

    public bool IsServerEndpoint(IPEndPoint endpoint);
}
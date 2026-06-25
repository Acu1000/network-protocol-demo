using System;
using System.Net;
using Protocol.Shared.Network.Packets;

namespace Protocol.Server.Network;

public interface IServerSessionManager
{
    public void SendToClient<T>(IPacket<T> packet, UInt16 clientId) where T : IPacket<T>;
    public void SendToAllClients<T>(IPacket<T> packet) where T : IPacket<T>;
    public void SendToAllClientsExcept<T>(IPacket<T> packet, UInt16 skippedClientId) where T : IPacket<T>;

    public void DisconnectClient(UInt16 clientId);

    public void HandleConnectRequestPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint);
    public void HandleDisconnectRequestPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint);
    public void HandlePingPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint);
    public void HandlePongPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint);

    public bool TryGetClientId(IPEndPoint sourceEndPoint, out UInt16 clientId);

    public void Process();
}
using Godot;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Server.Network;

public partial class ServerNetworkContext : Node
{
    private readonly ServerUdpHandler _udpHandler = new(12345);
    private readonly PacketRouter _router = new();
    private readonly ServerSessionManager _sessionManager = new();

    public override void _Ready()
    {
        _router.AddHandler(PacketType.ConnectRequest, _sessionManager.HandleConnectRequestPacket);
        
        _udpHandler.StartListening();
    }
    
    public override void _Process(double delta)
    {
        _udpHandler.RoutePackets(_router);
    }
}
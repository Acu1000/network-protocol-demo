using Godot;
using Protocol.Server.Network;

namespace Protocol.Server;

public partial class TestServerNode : Node
{
    private readonly ServerUdpHandler _udpHandler = new(12345);
    private readonly ServerPacketRouter _router = new();

    public override void _Ready()
    {
        _udpHandler.StartListening();
    }
    
    public override void _Process(double delta)
    {
        _udpHandler.RoutePackets(_router);
    }
}
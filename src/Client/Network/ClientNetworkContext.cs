using System.Net;
using Godot;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client.Network;

public partial class ClientNetworkContext : Node
{
    private readonly UdpHandler _udpHandler = new(54321);
    private readonly PacketRouter _router = new();

    private IPEndPoint _serverEndPoint = new(IPAddress.Parse("127.0.0.1"), 12345);
    
    public override void _Ready()
    {
        _udpHandler.StartListening();
        
        _router.AddHandler(PacketType.Pong, (span, point) => GD.Print("CLIENT: Pong received"));
    }
    
    public override void _Process(double delta)
    {
        _udpHandler.RoutePackets(_router);
        
        if (Input.IsActionJustPressed("ui_accept"))
        {
            byte[] buffer = new byte[ConnectRequestPacket.PacketMinSize];
            ConnectRequestPacket packet = new ConnectRequestPacket(123456);
            packet.WriteBytesTo(buffer);
            
            _udpHandler.Send(buffer, _serverEndPoint);
        }

        if (Input.IsActionJustPressed("ui_focus_next"))
        {
            byte[] buffer = new byte[PingPacket.PacketMinSize];
            PingPacket packet = new PingPacket();
            packet.WriteBytesTo(buffer);
            
            _udpHandler.Send(buffer, _serverEndPoint);
        }
    }
}
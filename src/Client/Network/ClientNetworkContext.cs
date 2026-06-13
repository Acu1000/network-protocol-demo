using Godot;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client.Network;

public partial class ClientNetworkContext : Node
{
    private readonly ClientUdpHandler _udpHandler = new("127.0.0.1", 12345, 54321);
    private readonly PacketRouter _router = new();

    public override void _Ready()
    {
        _udpHandler.StartListening();
    }
    
    public override void _Process(double delta)
    {
        /*PingPacket packet = new PingPacket();

        byte[] data = new byte[Marshal.SizeOf(typeof(PingPacket))];

        MemoryMarshal.Write(data, packet);

        _udpHandler.SendToServer(data);*/

        if (Input.IsActionJustPressed("ui_accept"))
        {
            byte[] buffer = new byte[ConnectRequestPacket.PacketMinSize];

            ConnectRequestPacket packet = new ConnectRequestPacket(123456);
            
            packet.WriteBytesTo(buffer);
            
            _udpHandler.SendToServer(buffer);
        }
    }
}
using System.Runtime.InteropServices;
using Godot;
using Protocol.Client.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client;

public partial class TestClientNode : Node
{
    private readonly ClientUdpHandler _udpHandler = new("127.0.0.1", 12345, 54321);
    private readonly ClientPacketRouter _router = new();

    public override void _Ready()
    {
        _udpHandler.StartListening();
    }
    
    public override void _Process(double delta)
    {
        PingPacket packet = new PingPacket();

        byte[] data = new byte[Marshal.SizeOf(typeof(PingPacket))];
        
        MemoryMarshal.Write(data, packet);
        
        _udpHandler.SendToServer(data);
    }
}
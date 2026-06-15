using System.Net;
using Godot;
using Protocol.Server;
using Protocol.Shared.Entities;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client.Network;

public partial class ClientNetworkContext : Node
{
	private readonly UdpHandler _udpHandler = new(54321);
	private readonly PacketRouter _router = new();
	private readonly ClientEntityManager _clientEntityManager;

	// TODO: assign dynamically instead via session manager
	private IPEndPoint _serverEndPoint = new(IPAddress.Parse("127.0.0.1"), 12345);
		
	private SampleEntity sampleEntityC = new();
	private SampleEntity sampleEntityS = new();

	public ClientNetworkContext()
	{
		_clientEntityManager = new ClientEntityManager(_udpHandler);
	}
	
	public override void _Ready()
	{
		_clientEntityManager.AddEntityLocal(123, sampleEntityC);
		_clientEntityManager.AddEntityLocal(456, sampleEntityS);
		
		_udpHandler.StartListening();
		
		_router.AddHandler(PacketType.Pong, (_, _) => GD.Print("CLIENT: Pong received"));
		_router.AddHandler(PacketType.SingleEntityUpdate, _clientEntityManager.HandleSingleEntityUpdatePacket);
	}
	
	public override void _Process(double delta)
	{
		sampleEntityC.Counter++;
		GD.Print("CLIENT: S = " + sampleEntityS.Counter + ", C = " + sampleEntityC.Counter);
		
		_udpHandler.RoutePackets(_router);
		
		_clientEntityManager.Process();
		
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

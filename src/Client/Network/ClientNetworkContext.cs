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

	private readonly ClientSessionManager _sessionManager;
	private readonly ClientEntityManager _clientEntityManager;

	private readonly IPEndPoint _serverEndPoint = new(IPAddress.Parse("127.0.0.1"), 12345);

	private SampleEntity sampleEntityC = new();
	private SampleEntity sampleEntityS = new();

	public ClientNetworkContext()
	{
		_sessionManager = new ClientSessionManager(_udpHandler);
		_clientEntityManager = new ClientEntityManager(_udpHandler);
	}

	public override void _Ready()
	{
		GD.Print("CLIENT: Ready");
		GD.Print("CLIENT: ui_accept = connect");
		GD.Print("CLIENT: ui_focus_next = send ping");
		GD.Print("CLIENT: ui_cancel = disconnect");

		_clientEntityManager.AddEntityLocal(123, sampleEntityC);
		_clientEntityManager.AddEntityLocal(456, sampleEntityS);

		_router.AddHandler(PacketType.ConnectAccept, _sessionManager.HandleConnectAcceptPacket);
		_router.AddHandler(PacketType.DisconnectAccept, _sessionManager.HandleDisconnectAcceptPacket);
		_router.AddHandler(PacketType.Ping, _sessionManager.HandlePingPacket);
		_router.AddHandler(PacketType.Pong, _sessionManager.HandlePongPacket);
		_router.AddHandler(PacketType.SingleEntityUpdate, _clientEntityManager.HandleSingleEntityUpdatePacket);

		_udpHandler.StartListening();
	}

	public override void _Process(double delta)
	{
		sampleEntityC.Counter++;

		_udpHandler.RoutePackets(_router);

		_clientEntityManager.Process();

		if (Input.IsActionJustPressed("ui_accept"))
		{
			ConnectToServer();
		}

		if (Input.IsActionJustPressed("ui_focus_next"))
		{
			SendPingToServer();
		}

		if (Input.IsActionJustPressed("ui_cancel"))
		{
			DisconnectFromServer();
		}
	}

	private async void ConnectToServer()
	{
		bool connected = await _sessionManager.TryConnectToServer(_serverEndPoint);

		if (connected)
		{
			GD.Print("CLIENT TEST: Connection successful");
		}
		else
		{
			GD.PrintErr("CLIENT TEST: Connection failed");
		}
	}

	private void SendPingToServer()
	{
		GD.Print("CLIENT TEST: Sending Ping to server");
		_sessionManager.SendToServer(new PingPacket());
	}

	private void DisconnectFromServer()
	{
		GD.Print("CLIENT TEST: Disconnecting from server");
		_sessionManager.DisconnectFromServer();
	}
}

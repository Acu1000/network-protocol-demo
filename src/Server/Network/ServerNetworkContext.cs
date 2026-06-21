using System;
using Godot;
using Protocol.Shared.Entities;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Server.Network;

public partial class ServerNetworkContext : Node
{
	private readonly UdpHandler _udpHandler = new(12345);
	private readonly PacketRouter _router = new();
	private readonly ServerSessionManager _sessionManager;
	private readonly ServerEntityManager _serverEntityManager;

	private SampleEntity sampleEntityC = new();
	private SampleEntity sampleEntityS = new();

	public ServerNetworkContext()
	{
		_sessionManager = new ServerSessionManager(_udpHandler);
		_serverEntityManager = new ServerEntityManager(_udpHandler);
	}

	public override void _Ready()
	{
		GD.Print("SERVER: Ready");
		GD.Print("SERVER: ui_page_up = send Ping to all clients");
		GD.Print("SERVER: ui_page_down = send Ping to all clients except first connected client");

		_serverEntityManager.AddEntityLocal(123, sampleEntityC);
		_serverEntityManager.AddEntityLocal(456, sampleEntityS);

		_router.AddHandler(PacketType.ConnectRequest, _sessionManager.HandleConnectRequestPacket);
		_router.AddHandler(PacketType.DisconnectRequest, _sessionManager.HandleDisconnectRequestPacket);
		_router.AddHandler(PacketType.Ping, _sessionManager.HandlePingPacket);
		_router.AddHandler(PacketType.Pong, _sessionManager.HandlePongPacket);
		_router.AddHandler(PacketType.SingleEntityUpdate, _serverEntityManager.HandleSingleEntityUpdatePacket);

		_udpHandler.StartListening();
	}

	public override void _Process(double delta)
	{
		sampleEntityS.Counter++;

		_udpHandler.RoutePackets(_router);

		_serverEntityManager.Process();

		if (Input.IsActionJustPressed("ui_page_up"))
		{
			GD.Print("SERVER TEST: Send Ping to all clients");
			_sessionManager.SendToAllClients(new PingPacket());
		}

		if (Input.IsActionJustPressed("ui_page_down"))
		{
			if (_sessionManager.Clients.Count == 0)
			{
				GD.PrintErr("SERVER TEST: Cannot test SendToAllClientsExcept. No clients connected.");
				return;
			}

			UInt16 skippedClientId = _sessionManager.Clients[0].ClientId;

			GD.Print($"SERVER TEST: Send Ping to all clients except ClientId {skippedClientId}");
			_sessionManager.SendToAllClientsExcept(new PingPacket(), skippedClientId);
		}
	}
}

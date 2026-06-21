using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Godot;
using Protocol.Server;
using Protocol.Shared.Entities;
using Protocol.Shared.EntityHandlers;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client.Network;

public partial class ClientNetworkContext : Node
{
	private readonly UdpHandler _udpHandler;
	private readonly PacketRouter _router = new();

	private readonly ClientSessionManager _sessionManager;
	private readonly ClientEntityManager _clientEntityManager;
	private readonly IClientSessionManager _clientSessionManager;
	
	[Export] private Godot.Collections.Array<Node> _entityHandlers;

	private readonly IPEndPoint _serverEndPoint = new(IPAddress.Parse("127.0.0.1"), 12345);

	// TODO: assign dynamically instead via session manager
	private IPEndPoint _serverEndPoint = new(IPAddress.Parse("127.0.0.1"), 12345);
	
	public ClientNetworkContext()
	{		
		_udpHandler = new(54321);
		_clientSessionManager = new MockClientSessionManager(_udpHandler, port);
		_clientEntityManager = new ClientEntityManager(_clientSessionManager);
	}

	public override void _Ready()
	{

		GD.Print("CLIENT: Ready");
		GD.Print("CLIENT: ui_accept = connect");
		GD.Print("CLIENT: ui_focus_next = send ping");
		GD.Print("CLIENT: ui_cancel = disconnect");

		foreach (var handlerNode in _entityHandlers)
		{
			if (handlerNode is not IEntityHandler handler) throw new Exception("Node is not an entity handler");
			_clientEntityManager.AddEntityHandler(handler.GetEntityType(), handler);
		}
		
		_udpHandler.StartListening();
		
    _router.AddHandler(PacketType.ConnectAccept, _sessionManager.HandleConnectAcceptPacket);
		_router.AddHandler(PacketType.DisconnectAccept, _sessionManager.HandleDisconnectAcceptPacket);
		_router.AddHandler(PacketType.Ping, _sessionManager.HandlePingPacket);
		_router.AddHandler(PacketType.Pong, _sessionManager.HandlePongPacket);
		_router.AddHandler(PacketType.SingleEntityUpdate, _clientEntityManager.HandleSingleEntityUpdatePacket);
		_router.AddHandler(PacketType.SingleEntityCreate, _clientEntityManager.HandleSingleEntityCreatePacket);
		_router.AddHandler(PacketType.SingleEntitySnapshot, _clientEntityManager.HandleSingleEntitySnapshotPacket);
		_router.AddHandler(PacketType.SetEntityOwner, _clientEntityManager.HandleSetEntityOwnerPacket);
	}

	public override void _Process(double delta)
	{
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

		_udpHandler.RoutePackets(_router);
		_clientEntityManager.Process();
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

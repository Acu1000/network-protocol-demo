using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Godot;
using Godot.Collections;
using Protocol.Server;
using Protocol.Shared.Entities;
using Protocol.Shared.Models;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;
using Array = Godot.Collections.Array;

namespace Protocol.Client.Network;

public partial class ClientNetworkContext : Node
{
	private readonly UdpHandler _udpHandler;
	private readonly PacketRouter _router = new();

	private readonly ClientEntityManager _clientEntityManager;
	private readonly ClientSessionManager _clientSessionManager;
	private readonly EntityFactory _entityFactory;
	
	[Export] public String ServerEndPoint = "127.0.0.1";

	[ExportCategory("Entity Spawn Config")]
	[Export] public Array<EntityType> SpawnConfigEntityType;
	[Export] public Array<PackedScene> SpawnConfigPrefab;
	[Export] public Array<Node> SpawnConfigParent;

	private IPEndPoint _serverEndPoint;
	
	public ClientNetworkContext()
	{		
		_udpHandler = new(0);
		_clientSessionManager = new ClientSessionManager(_udpHandler);
		_entityFactory = new EntityFactory();
		_clientEntityManager = new ClientEntityManager(_clientSessionManager, _entityFactory);
	}

	public override void _Ready()
	{
		_serverEndPoint = new IPEndPoint(IPAddress.Parse(ServerEndPoint), 12345);
		
		_udpHandler.StartListening();
		
		_router.AddHandler(PacketType.ConnectAccept, _clientSessionManager.HandleConnectAcceptPacket);
		_router.AddHandler(PacketType.DisconnectAccept, _clientSessionManager.HandleDisconnectAcceptPacket);
		_router.AddHandler(PacketType.Ping, _clientSessionManager.HandlePingPacket);
		_router.AddHandler(PacketType.Pong, _clientSessionManager.HandlePongPacket);
		_router.AddHandler(PacketType.SingleEntityUpdate, _clientEntityManager.HandleSingleEntityUpdatePacket);
		_router.AddHandler(PacketType.SingleEntityCreate, _clientEntityManager.HandleSingleEntityCreatePacket);
		_router.AddHandler(PacketType.SingleEntityDelete, _clientEntityManager.HandleSingleEntityDeletePacket);
		_router.AddHandler(PacketType.SingleEntitySnapshot, _clientEntityManager.HandleSingleEntitySnapshotPacket);
		_router.AddHandler(PacketType.SetEntityOwner, _clientEntityManager.HandleSetEntityOwnerPacket);

		if (SpawnConfigEntityType.Count != SpawnConfigParent.Count ||
		    SpawnConfigEntityType.Count != SpawnConfigPrefab.Count)
		{
			GD.PrintErr("Spawn config array sizes don't match!");
			GetTree().Quit(1);
		}

		for (int i = 0; i < SpawnConfigEntityType.Count; i++)
		{
			_entityFactory.AddSpawnConfig(SpawnConfigEntityType[i], SpawnConfigPrefab[i], SpawnConfigParent[i]);
		}
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
		bool connected = await _clientSessionManager.TryConnectToServer(_serverEndPoint);

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
		_clientSessionManager.SendToServer(new PingPacket());
	}

	private void DisconnectFromServer()
	{
		GD.Print("CLIENT TEST: Disconnecting from server");
		_clientSessionManager.DisconnectFromServer();
	}
}

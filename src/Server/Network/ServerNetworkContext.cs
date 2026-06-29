using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Godot;
using Protocol.Server.Network.Models;
using Protocol.Shared.Entities;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;
using Protocol.Shared.Scenes.Player;

namespace Protocol.Server.Network;

public partial class ServerNetworkContext : Node
{
	public static ServerNetworkContext? Instance { get; private set; } = null;
	
	private readonly UdpHandler _udpHandler = new UdpHandler(12345);
	//private readonly UdpHandler _udpHandler = new UnreliableUdpHandler(12345, 0.2f, 50);
	private readonly PacketRouter _router = new();
	private readonly ServerSessionManager _serverSessionManager;
	private readonly ServerEntityManager _serverEntityManager;
	private readonly ServerRemoteProcedureManager _serverRemoteProcedureManager;
  
	[Export] public double SnapshotInterval = 1.0f;
	
	[Export] public PackedScene PlayerCharacterPrefab;
	[Export] public Node PlayerCharacterContainer;
	  
	private double _snapshotTimer;
	  
	private long frame = 0;

	private Dictionary<UInt16, PlayerCharacter> _playerCharacters = new();

	[Export] private PackedScene _bulletPrefab;
	[Export] private Node2D _bulletContainer;

	public ServerNetworkContext()
	{
		Instance = this;
		
		_snapshotTimer = SnapshotInterval;
		_serverSessionManager = new ServerSessionManager(_udpHandler, _router);
		_serverEntityManager = new ServerEntityManager(_serverSessionManager);
		_serverRemoteProcedureManager = new ServerRemoteProcedureManager(_serverSessionManager);

		_serverSessionManager.ClientConnected += OnClientConnected;
	}

	public override void _Ready()
	{
		GD.Print("SERVER: Ready");
		GD.Print("SERVER: ui_page_up = send Ping to all clients");
		GD.Print("SERVER: ui_page_down = send Ping to all clients except first connected client");

		_router.AddHandler(PacketType.ConnectRequest, _serverSessionManager.HandleConnectRequestPacket);
		_router.AddHandler(PacketType.DisconnectRequest, _serverSessionManager.HandleDisconnectRequestPacket);
		_router.AddHandler(PacketType.Ping, _serverSessionManager.HandlePingPacket);
		_router.AddHandler(PacketType.Pong, _serverSessionManager.HandlePongPacket);
		_router.AddHandler(PacketType.SingleEntityUpdate, _serverEntityManager.HandleSingleEntityUpdatePacket);
		_router.AddHandler(PacketType.RemoteProcedureCall, _serverRemoteProcedureManager.HandleRpcPacket);
		
		_serverRemoteProcedureManager.AddProcedure("shoot", ShootRemoteProcedure);

		_udpHandler.StartListening();

		foreach (Node node in GetTree().GetNodesInGroup("Entity"))
		{
			if (!(node is IEntity entity))
			{
				GD.PrintErr($"Node {node.Name} in Entity group is not an IEntity!");
				continue;
			}
			
			_serverEntityManager.AddEntityGlobal(entity);
		}
	}

	public override void _Process(double delta)
	{
		_serverSessionManager.Process();
		_serverEntityManager.Process();

		if (Input.IsActionJustPressed("ui_page_up"))
		{
			GD.Print("SERVER TEST: Send Ping to all clients");
			_serverSessionManager.SendToAllClients(new PingPacket());
		}

		_snapshotTimer -= delta;
		if (_snapshotTimer <= 0.0f)
		{
			_snapshotTimer += SnapshotInterval;
			_serverEntityManager.SendSnapshotToAll();
		}
		
		_serverEntityManager.Process();
	}
	
	private void OnClientConnected(ClientInfo client)
	{
		PlayerCharacter character = (PlayerCharacter)PlayerCharacterPrefab.Instantiate();
		UInt64 id = _serverEntityManager.AddEntityGlobal(character);
		_serverEntityManager.SetEntityNetworkOwner(id, client.ClientId);
		PlayerCharacterContainer.AddChild(character);
		_playerCharacters.Add(client.ClientId, character);
	}
	
	// TODO: move to a proper location
	private void ShootRemoteProcedure(UInt16 callerId, ReadOnlySpan<byte> args)
	{
		if (_playerCharacters.TryGetValue(callerId, out var character))
		{
			Bullet bullet = (Bullet)_bulletPrefab.Instantiate();
			bullet.GlobalPosition = character.GlobalPosition + character.Turret.GlobalTransform.X * 0.5f;
			bullet.Velocity = character.Turret.GlobalTransform.X * 6.0f;
			_serverEntityManager.AddEntityGlobal(bullet);
			_bulletContainer.AddChild(bullet);
		}
		else
		{
			GD.Print("CHARACTER NOT FOUND");
		}
	}
}


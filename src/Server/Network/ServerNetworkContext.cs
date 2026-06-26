using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Godot;
using Protocol.Shared.Entities;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;
using Protocol.Shared.Scenes.Player;

namespace Protocol.Server.Network;

public partial class ServerNetworkContext : Node
{
	private readonly UdpHandler _udpHandler = new(12345);
	private readonly PacketRouter _router = new();
	private readonly ServerSessionManager _serverSessionManager;
	private readonly ServerEntityManager _serverEntityManager;
  
	[Export] public double SnapshotInterval = 1.0f;
	
	[Export] public PlayerCharacter Char1;
	[Export] public PlayerCharacter Char2;
	[Export] public BasicEnemy Enemy;
	  
	private double _snapshotTimer;
	  
	private long frame = 0;

	public ServerNetworkContext()
	{
		_snapshotTimer = SnapshotInterval;
		_serverSessionManager = new ServerSessionManager(_udpHandler, _router);
		_serverEntityManager = new ServerEntityManager(_serverSessionManager);
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

		_udpHandler.StartListening();
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
    
	    frame++;
	    if (frame == 5)
		{
		  Char1.GlobalPosition = new Vector2(-5, 0);
		  //Char1.NetworkOwnerId = 1;
		  var playerid1 = _serverEntityManager.AddEntityGlobal(Char1);
		  _serverEntityManager.SetEntityNetworkOwner(playerid1, 1);
		  
		  Char2.GlobalPosition = new Vector2(5, 0);
		  //Char2.NetworkOwnerId = 2;
		  var playerid2 = _serverEntityManager.AddEntityGlobal(Char2);
		  _serverEntityManager.SetEntityNetworkOwner(playerid2, 2);

		  Enemy.GlobalPosition = new Vector2(0, -5);
		  _serverEntityManager.AddEntityGlobal(Enemy);
		}
		/*if (frame == 100)
		{
		    _serverEntityManager.SetEntityNetworkOwner(playerid, 1);
		}*/

		_snapshotTimer -= delta;
		if (_snapshotTimer <= 0.0f)
		{
			_snapshotTimer += SnapshotInterval;
			_serverEntityManager.SendSnapshotToAll();
		}
		
		_serverEntityManager.Process();
	}
}

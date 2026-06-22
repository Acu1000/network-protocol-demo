using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Godot;
using Protocol.Shared.Entities;
using Protocol.Shared.EntityHandlers;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Server.Network;

public partial class ServerNetworkContext : Node
{
	private readonly UdpHandler _udpHandler = new(12345);
	private readonly PacketRouter _router = new();
	private readonly IServerSessionManager _serverSessionManager;
	private readonly ServerEntityManager _serverEntityManager;

	private SampleEntity sampleEntityC = new();
	private SampleEntity sampleEntityS = new();
  
	[Export] private Godot.Collections.Array<Node> _entityHandlers;
	[Export] public double SnapshotInterval = 1.0f;
	  
	private double _snapshotTimer;
	  
	private long frame = 0;

	public ServerNetworkContext()
	{
		_snapshotTimer = SnapshotInterval;
		_serverSessionManager = new ServerSessionManager(_udpHandler);
		_serverEntityManager = new ServerEntityManager(_serverSessionManager);
	}

	public override void _Ready()
	{
		GD.Print("SERVER: Ready");
		GD.Print("SERVER: ui_page_up = send Ping to all clients");
		GD.Print("SERVER: ui_page_down = send Ping to all clients except first connected client");

		_serverEntityManager.AddEntityLocal(123, sampleEntityC);
		_serverEntityManager.AddEntityLocal(456, sampleEntityS);

		_router.AddHandler(PacketType.ConnectRequest, _serverSessionManager.HandleConnectRequestPacket);
		_router.AddHandler(PacketType.DisconnectRequest, _serverSessionManager.HandleDisconnectRequestPacket);
		_router.AddHandler(PacketType.Ping, _serverSessionManager.HandlePingPacket);
		_router.AddHandler(PacketType.Pong, _serverSessionManager.HandlePongPacket);
		_router.AddHandler(PacketType.SingleEntityUpdate, _serverEntityManager.HandleSingleEntityUpdatePacket);

    foreach (var handlerNode in _entityHandlers)
    {
        if (handlerNode is not IEntityHandler handler) throw new Exception("Node is not an entity handler");
        _serverEntityManager.AddEntityHandler(handler.GetEntityType(), handler);
    }

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
			_serverSessionManager.SendToAllClients(new PingPacket());
		}
    
	    frame++;
	    if (frame == 5)
		{
		  PlayerCharacterEntity character = new();
		  var playerid = _serverEntityManager.AddEntityGlobal(character);
		  _serverEntityManager.SetEntityNetworkOwner(playerid, 1);

		  BasicEnemyEntity enemy = new();
		  enemy.PositionX = 3.0f;
		  enemy.PositionY = 3.0f;
		  _serverEntityManager.AddEntityGlobal(enemy);
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

		_udpHandler.RoutePackets(_router);

		_serverEntityManager.Process();
	}
}

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
	private readonly ClientEntityManager _clientEntityManager;
	private readonly IClientSessionManager _clientSessionManager;
	
	[Export] private Godot.Collections.Array<Node> _entityHandlers;

	// TODO: assign dynamically instead via session manager
	private IPEndPoint _serverEndPoint = new(IPAddress.Parse("127.0.0.1"), 12345);
	
	public ClientNetworkContext()
	{
		int port = OS.GetCmdlineArgs().Contains("--first") ? 54321 : 54322;
		
		_udpHandler = new(54321);
		_clientSessionManager = new MockClientSessionManager(_udpHandler, port);
		_clientEntityManager = new ClientEntityManager(_clientSessionManager);
	}
	
	public override void _Ready()
	{
		foreach (var handlerNode in _entityHandlers)
		{
			if (handlerNode is not IEntityHandler handler) throw new Exception("Node is not an entity handler");
			_clientEntityManager.AddEntityHandler(handler.GetEntityType(), handler);
		}
		
		_udpHandler.StartListening();
		
		_router.AddHandler(PacketType.Pong, (_, _) => GD.Print("CLIENT: Pong received"));
		_router.AddHandler(PacketType.SingleEntityUpdate, _clientEntityManager.HandleSingleEntityUpdatePacket);
		_router.AddHandler(PacketType.SingleEntityCreate, _clientEntityManager.HandleSingleEntityCreatePacket);
		_router.AddHandler(PacketType.SingleEntitySnapshot, _clientEntityManager.HandleSingleEntitySnapshotPacket);
		_router.AddHandler(PacketType.SetEntityOwner, _clientEntityManager.HandleSetEntityOwnerPacket);
	}
	
	public override void _Process(double delta)
	{
		_udpHandler.RoutePackets(_router);
		_clientEntityManager.Process();
	}
}

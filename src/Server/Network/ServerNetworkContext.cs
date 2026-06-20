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
    private readonly ServerSessionManager _sessionManager;
    private readonly ServerEntityManager _serverEntityManager;
    
    [Export] private Godot.Collections.Array<Node> _entityHandlers;
    
    private SampleEntity sampleEntityC = new();
    private SampleEntity sampleEntityS = new();
    private long frame = 0;
    
    public ServerNetworkContext()
    {
        _sessionManager = new ServerSessionManager(_udpHandler);
        _serverEntityManager = new ServerEntityManager(_udpHandler);
    }
    
    public override void _Ready()
    {
        _serverEntityManager.AddEntityLocal(123, sampleEntityC);
        _serverEntityManager.AddEntityLocal(456, sampleEntityS);
        
        foreach (var handlerNode in _entityHandlers)
        {
            if (handlerNode is not IEntityHandler handler) throw new Exception("Node is not an entity handler");
            _serverEntityManager.AddEntityHandler(handler.GetEntityType(), handler);
        }
        
        _router.AddHandler(PacketType.ConnectRequest, _sessionManager.HandleConnectRequestPacket);
        _router.AddHandler(PacketType.Ping, _sessionManager.HandlePingPacket);
        _router.AddHandler(PacketType.SingleEntityUpdate, _serverEntityManager.HandleSingleEntityUpdatePacket);
        
        _udpHandler.StartListening();
    }

    private ulong playerid = 0;
    
    public override void _Process(double delta)
    {
        frame++;
        if (frame == 50)
        {
            PlayerCharacterEntity character = new();
            playerid = _serverEntityManager.AddEntityGlobal(character);
        }
        if (frame == 100)
        {
            _serverEntityManager.SetEntityNetworkOwner(playerid, 1);
        }
        
        sampleEntityS.Counter++;
        //GD.Print("SERVER: S = " + sampleEntityS.Counter + ", C = " + sampleEntityC.Counter);
        
        _udpHandler.RoutePackets(_router);
        
        _serverEntityManager.Process();
    }
}
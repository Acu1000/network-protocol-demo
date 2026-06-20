using System;
using System.Net;
using System.Threading.Tasks;
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
        _serverEntityManager.AddEntityLocal(123, sampleEntityC);
        _serverEntityManager.AddEntityLocal(456, sampleEntityS);
        
        _router.AddHandler(PacketType.ConnectRequest, _sessionManager.HandleConnectRequestPacket);
        _router.AddHandler(PacketType.Ping, _sessionManager.HandlePingPacket);
        _router.AddHandler(PacketType.SingleEntityUpdate, _serverEntityManager.HandleSingleEntityUpdatePacket);
        
        _udpHandler.StartListening();

        Task.Run(() =>
        {
            Task.Delay(500);
            _serverEntityManager.SetEntityNetworkOwner(123, 1);
        });
    }
    
    public override void _Process(double delta)
    {
        sampleEntityS.Counter++;
        //GD.Print("SERVER: S = " + sampleEntityS.Counter + ", C = " + sampleEntityC.Counter);
        
        _udpHandler.RoutePackets(_router);
        
        _serverEntityManager.Process();
    }
}
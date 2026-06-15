using System;
using System.Net;
using Protocol.Shared.Entities;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Server;

public class ServerEntityManager : BaseEntityManager
{
    private readonly UdpHandler _udpHandler;

    public ServerEntityManager(UdpHandler udpHandler)
    {
        _udpHandler = udpHandler;
    }
    
    public void HandleSingleEntityUpdatePacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        // TODO (IMPORTANT): Validate the new state
        
        if (!SingleEntityUpdatePacket.TryParse(packetData, out SingleEntityUpdatePacket packet))
        {
            return;
        }
        
        UpdateEntity(packet.EntityID, packet.NewStateBytes);
    }

    private void UpdateEntity(UInt64 id, ReadOnlySpan<byte> stateBytes)
    {
        _entities[id].UpdateState(stateBytes);
    }
    
    public override void Process()
    {
        foreach (var kv in _entities)
        {
            UInt64 id = kv.Key;
            Entity entity = kv.Value;

            if (entity.StateChanged())
            {
                SingleEntityUpdatePacket packet = new SingleEntityUpdatePacket(
                    id, 
                    entity.EntityType, 
                    entity.GetState()
                    );
                
                // TODO: send to all connected clients instead
                _udpHandler.Send(
                    packet.ToBytes(), 
                    new IPEndPoint(IPAddress.Loopback, 54321));
            }
        }
    }
}
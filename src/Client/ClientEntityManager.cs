using System;
using System.Net;
using Godot;
using Protocol.Shared.Entities;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client;

public class ClientEntityManager : BaseEntityManager
{
    private readonly UdpHandler _udpHandler;

    public ClientEntityManager(UdpHandler udpHandler)
    {
        _udpHandler = udpHandler;
    }
    
    public void HandleSingleEntityUpdatePacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (!SingleEntityUpdatePacket.TryParse(packetData, out SingleEntityUpdatePacket packet))
        {
            return;
        }
        
        UpdateOrCreateEntity(packet.EntityID, packet.NewStateBytes);
    }
    
    private void UpdateOrCreateEntity(UInt64 id, ReadOnlySpan<byte> stateBytes)
    {
        GD.Print($"Updating entity {id}");
        
        if (_entities.TryGetValue(id, out var entity))
        {
            entity.UpdateState(stateBytes);
        }
        else
        {
            // TODO: create entity
            throw new NotImplementedException("Create entity");
        }
    }

    public override void Process()
    {
        foreach (var kv in _entities)
        {
            UInt64 id = kv.Key;
            Entity entity = kv.Value;
            
            
        }
    }
}
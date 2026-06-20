using System;
using System.Collections.Generic;
using System.Net;
using Godot;
using Protocol.Shared.Entities;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client;

public class ClientEntityManager : BaseEntityManager
{
    private readonly UdpHandler _udpHandler;
    
    private readonly HashSet<UInt64> _ownedEntities = new();

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

    public void HandleSetEntityOwnerPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (!SetEntityOwnerPacket.TryParse(packetData, out SetEntityOwnerPacket packet))
        {
            return;
        }

        if (_entities.TryGetValue(packet.EntityId, out var entity))
        {
            entity.NetworkOwnerId = packet.NewOwnerId;
        }

        // TODO: Check if matches own ClientId
        if (packet.NewOwnerId == 1)
        {
            _ownedEntities.Add(packet.EntityId);
        }
        else if (_ownedEntities.Contains(packet.EntityId) && packet.NewOwnerId != 1)
        {
            _ownedEntities.Remove(packet.EntityId);
        }
    }
    
    private void UpdateOrCreateEntity(UInt64 id, ReadOnlySpan<byte> stateBytes)
    {
        //GD.Print($"Updating entity {id}");
        
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
        foreach (var entityId in  _ownedEntities)
        {
            Entity entity = _entities.GetValueOrDefault(entityId);

            if (entity == null) continue;

            if (entity.StateChanged())
            {
                // TODO: add client packet header
                SingleEntityUpdatePacket packet = new SingleEntityUpdatePacket(
                    entityId,
                    entity.EntityType,
                    entity.GetState()
                    );
                
                // TODO: Get server endpoint from session manager
                _udpHandler.Send(packet.ToBytes(), new IPEndPoint(IPAddress.Loopback, 12345));
            }
        }
    }
}
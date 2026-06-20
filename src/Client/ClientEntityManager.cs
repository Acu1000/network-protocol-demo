using System;
using System.Collections.Generic;
using System.Net;
using Godot;
using Protocol.Shared.Entities;
using Protocol.Shared.EntityHandlers;
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
        
        if (_entities.TryGetValue(packet.EntityID, out var entity))
        {
            entity.UpdateState(packet.NewStateBytes);
            UpdateEntityLocal(packet.EntityID, entity);
        }
    }

    public void HandleSingleEntityCreatePacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (!SingleEntityCreatePacket.TryParse(packetData, out SingleEntityCreatePacket packet)) return;
        if (_entities.ContainsKey(packet.EntityId)) return;
        
        EntityType entityType = packet.EntityType;

        Entity entity = EntityFactory.CreateEntity(entityType);
        entity.UpdateState(packet.InitialStateBytes);
        
        AddEntityLocal(packet.EntityId, entity);
    }

    public void HandleSetEntityOwnerPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (!SetEntityOwnerPacket.TryParse(packetData, out SetEntityOwnerPacket packet))
        {
            return;
        }
        
        IEntityHandler handler = null;
        if (_entities.TryGetValue(packet.EntityId, out var entity))
        {
            entity.NetworkOwnerId = packet.NewOwnerId;
            handler = _entityHandlers.GetValueOrDefault(entity.EntityType);
        }
        
        // TODO: Check if matches own ClientId
        if (packet.NewOwnerId == 1)
        {
            _ownedEntities.Add(packet.EntityId);
            handler?.EntityOwnershipAcquired(packet.EntityId);
        }
        else if (_ownedEntities.Contains(packet.EntityId) && packet.NewOwnerId != 1)
        {
            _ownedEntities.Remove(packet.EntityId);
            handler?.EntityOwnershipLost(packet.EntityId);
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
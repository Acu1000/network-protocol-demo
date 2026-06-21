using System;
using System.Collections.Generic;
using System.Net;
using Godot;
using Protocol.Client.Network;
using Protocol.Shared.Entities;
using Protocol.Shared.EntityHandlers;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client;

public class ClientEntityManager : BaseEntityManager
{
    //private readonly UdpHandler _udpHandler;
    private readonly IClientSessionManager _clientSessionManager;
    
    private readonly HashSet<UInt64> _ownedEntities = new();

    /*public ClientEntityManager(UdpHandler udpHandler)
    {
        _udpHandler = udpHandler;
    }*/

    public ClientEntityManager(IClientSessionManager clientSessionManager)
    {
        _clientSessionManager = clientSessionManager;
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

    public void HandleSingleEntitySnapshotPacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (!SingleEntitySnapshotPacket.TryParse(packetData, out SingleEntitySnapshotPacket packet))
        {
            return;
        }

        // TODO: keep track of non-updated entities and delete them
        if (_entities.TryGetValue(packet.EntityId, out var entity))
        {
            entity.UpdateState(packet.StateBytes);
            var handler = _entityHandlers.GetValueOrDefault(packet.EntityType);
            handler?.EntityUpdated(packet.EntityId, entity);
        }
        else
        {
            // Entity does not exist; Create new
            Entity newEntity = EntityFactory.CreateEntity(packet.EntityType);
            newEntity.UpdateState(packet.StateBytes);
            AddEntityLocal(packet.EntityId, newEntity);
            var handler = _entityHandlers.GetValueOrDefault(packet.EntityType);
            handler?.EntityCreated(packet.EntityId, newEntity);
        }
        SetEntityOwnerLocal(packet.EntityId, packet.EntityNetworkOwnerId);
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
        
        SetEntityOwnerLocal(packet.EntityId, packet.NewOwnerId);
    }

    private void SetEntityOwnerLocal(UInt64 entityId, UInt16 newOwnerId)
    {
        IEntityHandler handler = null;
        if (_entities.TryGetValue(entityId, out var entity))
        {
            entity.NetworkOwnerId = newOwnerId;
            handler = _entityHandlers.GetValueOrDefault(entity.EntityType);
        }
        
        // TODO: Check if matches own ClientId
        if (newOwnerId == 1)
        {
            _ownedEntities.Add(entityId);
            handler?.EntityOwnershipAcquired(entityId);
        }
        else if (_ownedEntities.Contains(entityId) && newOwnerId != 1)
        {
            _ownedEntities.Remove(entityId);
            handler?.EntityOwnershipLost(entityId);
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
                _clientSessionManager.SendToServer(packet);
            }
        }
    }
}
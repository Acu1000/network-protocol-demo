using System;
using System.Collections.Generic;
using System.Net;
using Godot;
using Protocol.Client.Network;
using Protocol.Shared.Entities;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client;

public class ClientEntityManager : BaseEntityManager
{
    private readonly ClientSessionManager _clientSessionManager;
    private readonly EntityFactory _entityFactory;
    
    private readonly HashSet<UInt64> _ownedEntities = new();
    

    public ClientEntityManager(ClientSessionManager clientSessionManager, EntityFactory entityFactory)
    {
        _clientSessionManager = clientSessionManager;
        _entityFactory = entityFactory;
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
        }
        else
        {
            // Entity does not exist; Create new
            IEntity newEntity = _entityFactory.CreateEntity(packet.EntityType, packet.StateBytes);
            _entities.Add(packet.EntityId, newEntity);
        }
        SetEntityOwnerLocal(packet.EntityId, packet.EntityNetworkOwnerId);
    }

    public void HandleSingleEntityCreatePacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (!SingleEntityCreatePacket.TryParse(packetData, out SingleEntityCreatePacket packet)) return;
        if (_entities.ContainsKey(packet.EntityId)) return;
        
        EntityType entityType = packet.EntityType;

        IEntity entity = _entityFactory.CreateEntity(entityType, packet.InitialStateBytes);
        _entities.Add(packet.EntityId, entity);
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
        if (_entities.TryGetValue(entityId, out var entity))
        {
            entity.NetworkOwnerId = newOwnerId;
        }
        
        if (newOwnerId == _clientSessionManager.ClientId)
        {
            _ownedEntities.Add(entityId);
            entity?.OwnershipChanged(true);
        }
        else if (_ownedEntities.Contains(entityId) && newOwnerId != _clientSessionManager.ClientId)
        {
            _ownedEntities.Remove(entityId);
            entity?.OwnershipChanged(false);
        }
    }
    
    public override void Process()
    {
        foreach (var entityId in _ownedEntities)
        {
            IEntity? entity = _entities.GetValueOrDefault(entityId);

            if (entity == null) continue;

            if (entity.UpdateNeeded)
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
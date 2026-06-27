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

    // Starting at 1000 to prevent underflows
    private UInt32 _currentSnapshotId = 1000;
    
    // For example with it set to 1 snapshot 19 packets can still get accepted
    // during snapshot 20 but earlier ones won't
    private const int MaxBackwardsSnapshots = 1;

    // Entities that haven't received a snapshot update in this many snapshots
    // will be assumed to have been deleted
    private const int EntityExpirationSnapshots = 3;
    

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
        
        if (TryGetEntity(packet.EntityID, out var entity))
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

        // Drop stale snapshots
        if (packet.SnapshotId < _currentSnapshotId - MaxBackwardsSnapshots)
        {
            GD.Print("DROP STALE SNAPSHOT ", packet.SnapshotId);
            return;
        }
        
        // TODO: keep track of non-updated entities and delete them
        if (TryGetEntity(packet.EntityId, out var entity))
        {
            entity.UpdateState(packet.StateBytes);
            entity.LastSnapshotId = Math.Max(entity.LastSnapshotId, packet.SnapshotId);
        }
        else
        {
            // Entity does not exist; Create new
            IEntity newEntity = _entityFactory.CreateEntity(packet.EntityType, packet.EntityId, packet.StateBytes);
            AddEntityLocal(packet.EntityId, newEntity);
            entity.LastSnapshotId = Math.Max(entity.LastSnapshotId, packet.SnapshotId);
        }
        SetEntityOwnerLocal(packet.EntityId, packet.EntityNetworkOwnerId);
        
        // New snapshot
        if (packet.SnapshotId > _currentSnapshotId)
        {
            _currentSnapshotId = packet.SnapshotId;
            DeleteExpiredEntities();
        }
    }

    // Delete entities that haven't received snapshot recently
    // (assume they've been deleted but the delete packet was lost)
    private void DeleteExpiredEntities()
    {
        foreach (var kv in GetEntities())
        {
            UInt64 entityId = kv.Key;
            IEntity entity = kv.Value;

            if (entity.LastSnapshotId < _currentSnapshotId - EntityExpirationSnapshots 
                && entity.LastSnapshotId != 0) // Protect entities whose snapshot id hasn't been set yet
            {
                GD.Print("DELETING EXPIRED ENTITY");
                RemoveEntityLocal(entityId);
            }
        }
    }

    public void HandleSingleEntityCreatePacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (!SingleEntityCreatePacket.TryParse(packetData, out SingleEntityCreatePacket packet)) return;
        if (EntityExists(packet.EntityId)) return;
        
        EntityType entityType = packet.EntityType;
        UInt64 entityId = packet.EntityId;

        IEntity entity = _entityFactory.CreateEntity(entityType, entityId, packet.InitialStateBytes);
        AddEntityLocal(entityId, entity);
    }

    public void HandleSingleEntityDeletePacket(ReadOnlySpan<byte> packetData, EndPoint sourceEndPoint)
    {
        if (!SingleEntityDeletePacket.TryParse(packetData, out SingleEntityDeletePacket packet)) return;
        if (!EntityExists(packet.EntityId)) return;
        
        RemoveEntityLocal(packet.EntityId);
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
        if (TryGetEntity(entityId, out var entity))
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
            if (!TryGetEntity(entityId, out IEntity entity)) continue;

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
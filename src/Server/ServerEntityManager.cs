using System;
using System.Collections.Generic;
using System.Net;
using Godot;
using Protocol.Server.Network;
using Protocol.Shared.Entities;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Server;

public class ServerEntityManager : BaseEntityManager
{
    //private readonly UdpHandler _udpHandler;
    private readonly ServerSessionManager _serverSessionManager;

    private UInt64 _nextEntityId = 1;
    private UInt32 _nextSnapshotId = 1;
    
    /*public ServerEntityManager(UdpHandler udpHandler)
    {
        _udpHandler = udpHandler;
    }*/

    public ServerEntityManager(ServerSessionManager serverSessionManager)
    {
        _serverSessionManager = serverSessionManager;
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

    public UInt64 AddEntityGlobal(Entity entity)
    {
        UInt64 id = _nextEntityId++;
        
        AddEntityLocal(id, entity);
        
        SingleEntityCreatePacket packet = new SingleEntityCreatePacket(
            id, 
            entity.EntityType, 
            entity.GetState());
        
        _serverSessionManager.SendToAllClients(packet);

        if (entity.NetworkOwnerId == 0)
        {
            _entityHandlers.GetValueOrDefault(entity.EntityType)?.EntityOwnershipAcquired(id);
        }
        
        return id;
    } 
    
    private void UpdateEntity(UInt64 id, ReadOnlySpan<byte> stateBytes)
    {
        _entities[id].UpdateState(stateBytes);
        UpdateEntityLocal(id, _entities[id]);
    }

    public void SetEntityNetworkOwner(UInt64 entityId, UInt16 newOwnerId)
    {
        if (_entities.TryGetValue(entityId, out var entity))
        {
            if (entity.NetworkOwnerId == newOwnerId) return;
            
            if (entity.NetworkOwnerId == 0)
            {
                _entityHandlers.GetValueOrDefault(entity.EntityType)?.EntityOwnershipLost(entityId);
            }
            
            entity.NetworkOwnerId = newOwnerId;
            
            SetEntityOwnerPacket packet = new(entityId, newOwnerId);
            _serverSessionManager.SendToClient(packet, newOwnerId);
        }
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

                if (entity.NetworkOwnerId == 0)
                {
                    _serverSessionManager.SendToAllClients(packet);
                }
                else
                {
                    _serverSessionManager.SendToAllClientsExcept(packet, entity.NetworkOwnerId);
                }
                
            }
        }
    }

    public void SendSnapshotToAll()
    {   
        foreach (var kv in _entities)
        {
            UInt64 entityId = kv.Key;
            Entity entity = kv.Value;

            SingleEntitySnapshotPacket packet = new SingleEntitySnapshotPacket(
                _nextSnapshotId++,
                entityId,
                entity.EntityType,
                entity.NetworkOwnerId,
                entity.GetState()
            );
            
            _serverSessionManager.SendToAllClients(packet);
        }
    }
}
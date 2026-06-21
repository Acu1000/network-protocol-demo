using System;
using Protocol.Shared.Entities;
using Protocol.Shared.Network;

namespace Protocol.Shared.EntityHandlers;

public interface IEntityHandler
{ 
    public EntityType GetEntityType();
    
    public void EntityCreated(UInt64 entityId, Entity entity);
    public void EntityUpdated(UInt64 entityId, Entity entity);
    public void EntityDeleted(UInt64 entityId);
    
    public void EntityOwnershipAcquired(UInt64 entityId);
    public void EntityOwnershipLost(UInt64 entityId);
}
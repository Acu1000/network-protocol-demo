using System;
using System.Collections.Generic;
using Godot;
using Protocol.Shared.Entities;
using Protocol.Shared.EntityHandlers;

namespace Protocol.Shared.Network;

public abstract class BaseEntityManager
{
    protected readonly Dictionary<UInt64, Entity> _entities = new();
    protected readonly Dictionary<EntityType, IEntityHandler> _entityHandlers = new();

    public abstract void Process();
    
    public bool EntityExists(UInt64 id) => _entities.ContainsKey(id);
    
    public Entity GetEntity(UInt64 id) => _entities[id];
    
    public void AddEntityLocal(UInt64 id, Entity entity)
    {
        _entities.Add(id, entity);
        if (_entityHandlers.TryGetValue(entity.EntityType, out IEntityHandler handler))
        {
            handler.EntityCreated(id, entity);
        }
    }

    public void UpdateEntityLocal(UInt64 id, Entity entity)
    {
        if (_entityHandlers.TryGetValue(entity.EntityType, out IEntityHandler handler))
        {
            handler.EntityUpdated(id, entity);
        }
    }
    
    public void RemoveEntityLocal(UInt64 id, EntityType entityType)
    {
        _entities.Remove(id);
        if (_entityHandlers.TryGetValue(entityType, out IEntityHandler handler))
        {
            handler.EntityDeleted(id);
        }
    }
    
    public void AddEntityHandler(EntityType entityType, IEntityHandler entityHandler)
    {
        _entityHandlers.Add(entityType, entityHandler);
    }
}
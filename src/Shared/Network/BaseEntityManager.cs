using System;
using System.Collections.Generic;
using Godot;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Network;

public abstract class BaseEntityManager
{
    private readonly Dictionary<UInt64, IEntity> _entities = new();
    //protected readonly Dictionary<EntityType, IEntityHandler> _entityHandlers = new();

    protected IEnumerable<KeyValuePair<UInt64, IEntity>> GetEntities() => _entities;
    
    protected bool TryGetEntity(UInt64 entityId, out IEntity? entity)
    {
        return _entities.TryGetValue(entityId, out entity);
    }
    
    protected bool EntityExists(UInt64 entityId)
    {
        return _entities.ContainsKey(entityId);
    }
    
    protected void AddEntityLocal(UInt64 entityId, IEntity entity)
    {
        _entities.Add(entityId, entity);
    }

    protected void RemoveEntityLocal(UInt64 entityId)
    {
        if (_entities.TryGetValue(entityId, out var entity))
        {
            // TODO: optimize it so it doesn't cyclically call this and .Deleted event
            _entities.Remove(entityId);
            entity.Delete();
        }
    }
    
    public abstract void Process();
}
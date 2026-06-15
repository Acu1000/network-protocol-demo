using System;
using System.Collections.Generic;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Network;

public abstract class BaseEntityManager
{
    protected readonly Dictionary<UInt64, Entity> _entities = new();

    public abstract void Process();
    
    public bool EntityExists(UInt64 id) => _entities.ContainsKey(id);
    
    public void AddEntityLocal(UInt64 id, Entity entity)
    {
        _entities.Add(id, entity);   
    }
    
    public void RemoveEntityLocal(UInt64 id)
    {
        _entities.Remove(id);
    }
}
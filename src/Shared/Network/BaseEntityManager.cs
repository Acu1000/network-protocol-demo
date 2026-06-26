using System;
using System.Collections.Generic;
using Godot;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Network;

public abstract class BaseEntityManager
{
    protected readonly Dictionary<UInt64, IEntity> _entities = new();
    //protected readonly Dictionary<EntityType, IEntityHandler> _entityHandlers = new();

    public abstract void Process();
}
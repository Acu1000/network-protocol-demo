using System;
using System.Collections.Generic;
using Godot;
using Protocol.Shared.Entities;
using Protocol.Shared.Network;
using Protocol.Shared.Scenes.Player;

namespace Protocol.Shared.EntityHandlers;

public partial class BasicEnemyEntityHandler : Node, IEntityHandler
{
    [Export] private PackedScene _prefab;
    [Export] private Node _instanceContainer;

    private readonly Dictionary<UInt64, BasicEnemy> _instances = new();

    public EntityType GetEntityType() => EntityType.BasicEnemy;

    public void EntityCreated(UInt64 entityId, Entity entity)
    {
        if (entity is not BasicEnemyEntity enemyEntity) return;
        BasicEnemy newInstance = (BasicEnemy)_prefab.Instantiate();
        
        newInstance.Entity = enemyEntity;
        newInstance.Position = new(enemyEntity.PositionX, enemyEntity.PositionY);
        
        _instances.Add(entityId, newInstance);
        _instanceContainer.AddChild(newInstance);
    }

    public void EntityUpdated(UInt64 entityId, Entity entity)
    {
        if (entity is not BasicEnemyEntity charEntity) return;
        if (!_instances.TryGetValue(entityId, out BasicEnemy instance)) return;
        
        instance.Position = new(charEntity.PositionX, charEntity.PositionY);
    }

    public void EntityDeleted(UInt64 entityId)
    {
        if (!_instances.TryGetValue(entityId, out BasicEnemy instance)) return;
        instance.QueueFree();
        _instances.Remove(entityId);
    }

    public void EntityOwnershipAcquired(UInt64 entityId)
    {
        if (!_instances.TryGetValue(entityId, out BasicEnemy instance)) return;
        instance.Controlled = true;
    }

    public void EntityOwnershipLost(UInt64 entityId)
    {
        if (!_instances.TryGetValue(entityId, out BasicEnemy instance)) return;
        instance.Controlled = false;
    }
}
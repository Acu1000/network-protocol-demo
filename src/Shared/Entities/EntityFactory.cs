using System;
using System.Collections.Generic;
using Godot;
using Protocol.Shared.Models;
using Array = Godot.Collections.Array;

namespace Protocol.Shared.Entities;

public class EntityFactory
{
    private readonly Dictionary<EntityType, EntitySpawnConfig> _spawnConfigs = new();

    public EntityFactory()
    {
    }
    
    public void AddSpawnConfig(EntityType entityType, PackedScene prefab, Node parent)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));
        if (parent is null) throw new ArgumentNullException(nameof(parent));
        
        _spawnConfigs.Add(entityType, new EntitySpawnConfig()
        {
            Prefab = prefab,
            Parent = parent,
        });
    }
    
    public IEntity CreateEntity(EntityType entityType, UInt64 entityId, ReadOnlySpan<byte> initialState)
    {
        EntitySpawnConfig? spawnConfig = _spawnConfigs.GetValueOrDefault(entityType);

        if (spawnConfig is null)
        {
            throw new ArgumentOutOfRangeException(nameof(entityType));
        }

        IEntity? entity = spawnConfig.Prefab.Instantiate() as IEntity;

        if (entity is null)
        {
            throw new Exception("Entity prefab does not implement IEntity");
        }
        
        entity.EntityId = entityId;
        entity.UpdateState(initialState);

        Node parent = spawnConfig.Parent;

        Node? entityNode = entity as Node;

        if (entityNode is null)
        {
            throw new Exception("Entity prefab is not a Node");
        }
        
        parent.AddChild(entityNode);

        return entity;
    }
}
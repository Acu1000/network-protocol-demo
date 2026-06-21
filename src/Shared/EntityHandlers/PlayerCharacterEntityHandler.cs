using System;
using System.Collections.Generic;
using Godot;
using Protocol.Shared.Entities;
using Protocol.Shared.Network;
using Protocol.Shared.Scenes.Player;

namespace Protocol.Shared.EntityHandlers;

public partial class PlayerCharacterEntityHandler : Node, IEntityHandler
{
    [Export] private PackedScene _playerCharacterEntityPrefab;
    [Export] private Node _characterContainer;

    private readonly Dictionary<UInt64, PlayerCharacter> _characters = new();

    public EntityType GetEntityType() => EntityType.PlayerCharacter;

    public void EntityCreated(UInt64 entityId, Entity entity)
    {
        PlayerCharacter newCharacter = (PlayerCharacter)_playerCharacterEntityPrefab.Instantiate();
        newCharacter.Entity = entity as PlayerCharacterEntity;
        _characters.Add(entityId, newCharacter);
        _characterContainer.AddChild(newCharacter);
    }

    public void EntityUpdated(UInt64 entityId, Entity entity)
    {
        if (entity is not PlayerCharacterEntity charEntity) return;
        if (!_characters.TryGetValue(entityId, out PlayerCharacter character)) return;

        // Don't update position if snapshot of own character is received close enough (prevents jittering)
        if (!character.Controlled ||
            (new Vector2(charEntity.PositionX, charEntity.PositionY).DistanceSquaredTo(character.Position) > 2.0f))
        {
            character.Position = new(charEntity.PositionX, charEntity.PositionY);
        }

    }

    public void EntityDeleted(UInt64 entityId)
    {
        if (!_characters.TryGetValue(entityId, out PlayerCharacter character)) return;
        character.QueueFree();
        _characters.Remove(entityId);
    }

    public void EntityOwnershipAcquired(UInt64 entityId)
    {
        if (!_characters.TryGetValue(entityId, out PlayerCharacter character)) return;
        character.Controlled = true;
    }

    public void EntityOwnershipLost(UInt64 entityId)
    {
        if (!_characters.TryGetValue(entityId, out PlayerCharacter character)) return;
        character.Controlled = false;
    }
}
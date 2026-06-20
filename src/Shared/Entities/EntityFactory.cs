using System;

namespace Protocol.Shared.Entities;

public static class EntityFactory
{
    public static Entity CreateEntity(EntityType entityType)
    {
        switch (entityType)
        {
            case EntityType.PlayerCharacter:
                return new PlayerCharacterEntity();
            
            case EntityType.SampleEntity:
                return new SampleEntity();
        }
        
        throw new ArgumentOutOfRangeException(nameof(entityType));
    }
}
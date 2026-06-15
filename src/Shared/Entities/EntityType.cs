using System;

namespace Protocol.Shared.Entities;

public enum EntityType : UInt16
{
    None = 0,
    
    SampleEntity,
    
    PlayerCharacter,
    Bullet,
}
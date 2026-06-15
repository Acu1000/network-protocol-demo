using System;

namespace Protocol.Shared.Entities;

public abstract class Entity
{
    public EntityType EntityType { get; }
    public bool IsOwned => true;

    public virtual bool StateChanged() => true;
    
    public abstract int StateSize { get; }
    
    public abstract void WriteStateTo(Span<byte> buffer);

    public byte[] GetState()
    {
        byte[] buffer = new byte[StateSize];
        WriteStateTo(buffer);
        return buffer;
    }
    
    public abstract void UpdateState(ReadOnlySpan<byte> state);

    protected Entity(EntityType entityType)
    {
        EntityType = entityType;
    }
}
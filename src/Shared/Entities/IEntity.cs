using System;

namespace Protocol.Shared.Entities;

public interface IEntity
{
    public EntityType EntityType { get; }


    public bool UpdateNeeded { get; }
    public void WriteStateTo(Span<byte> buffer);
    public byte[] GetState();
    public void UpdateState(ReadOnlySpan<byte> state);
    public void Delete();

    public UInt16 NetworkOwnerId { get; set; }
    public void OwnershipChanged(bool isOwned);
}
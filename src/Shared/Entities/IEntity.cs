using System;

namespace Protocol.Shared.Entities;

public interface IEntity
{
    public bool IsServer { get; set; }
    public UInt64? EntityId { get; set; }   
    public EntityType EntityType { get; }
    public UInt32 LastSnapshotId { get; set; }

    public event Action Deleted;
    
    public bool UpdateNeeded { get; }
    public void WriteStateTo(Span<byte> buffer);
    public byte[] GetState();
    public void UpdateState(ReadOnlySpan<byte> state);
    public void Delete();

    public UInt16 NetworkOwnerId { get; set; }
    public void OwnershipChanged(bool isOwned);
}
using System;
using Godot;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Scenes.Player;

public partial class Bullet : Area2D, IEntity
{
    private double _lifetime = 0;

    public Vector2 Velocity;
    
    public override void _PhysicsProcess(double delta)
    {
        if (IsServer)
        {
            _lifetime += delta;
            Position += Velocity * (float)delta;

            if (_lifetime > 0.3)
            {
                foreach (var body in GetOverlappingBodies())
                {
                    if (body is PlayerCharacter character)
                    {
                        GD.Print("HIT");
                        character.TakeDamage(1);
                    }

                    Delete();
                }
            }
        }
    }

    public bool IsServer { get; set; }
    public ulong? EntityId { get; set; }
    public EntityType EntityType => EntityType.Bullet;
    public uint LastSnapshotId { get; set; }
    public event Action? Deleted;
    public bool UpdateNeeded => true;
    
    public void WriteStateTo(Span<byte> buffer)
    {
        BitConverter.TryWriteBytes(buffer.Slice(0, 4), GlobalPosition.X);
        BitConverter.TryWriteBytes(buffer.Slice(4, 4), GlobalPosition.Y);
    }

    public byte[] GetState()
    {
        byte[] buffer = new byte[8];
        WriteStateTo(buffer);
        return buffer;
    }

    public void UpdateState(ReadOnlySpan<byte> state)
    {
        GlobalPosition = new Vector2(
            BitConverter.ToSingle(state.Slice(0, 4)),
            BitConverter.ToSingle(state.Slice(4, 4))
        );
    }

    public void Delete()
    {
        Deleted?.Invoke();
        QueueFree();
    }

    public ushort NetworkOwnerId { get; set; }
    public void OwnershipChanged(bool isOwned)
    { 
    }
}
using System;
using Godot;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Scenes.Player;

public partial class BasicEnemy : CharacterBody2D, IEntity
{
    [Export] private Area2D KillArea; 
    
    public const float Speed = 1.0f;

    public bool Controlled = false;
    
    private Vector2 _velocity;
    
    public override void _PhysicsProcess(double delta)
    {
        if (Controlled) ProcessControlled();
    }

    private void ProcessControlled()
    {
        foreach (var body in KillArea.GetOverlappingBodies())
        {
            if (body is PlayerCharacter character)
            {
                character.Die();
            } 
        }
        
        if (Random.Shared.Next(120) == 1)
        {
            _velocity = new Vector2(
                Random.Shared.NextSingle() * Speed * 2 - Speed,
                Random.Shared.NextSingle() * Speed * 2 - Speed
            ).LimitLength(Speed);
        }

        Velocity = _velocity;
        MoveAndSlide();
    }

    public UInt64? EntityId { get; set; }
    public EntityType EntityType => EntityType.BasicEnemy;
    public event Action? Deleted;
    public bool UpdateNeeded => true;
    
    public void WriteStateTo(Span<byte> buffer)
    {
        BitConverter.TryWriteBytes(buffer.Slice(0, 4), Position.X);
        BitConverter.TryWriteBytes(buffer.Slice(4, 4), Position.Y);
    }

    public byte[] GetState()
    {
        byte[] buffer = new byte[8];
        WriteStateTo(buffer);
        return buffer;
    }

    public void UpdateState(ReadOnlySpan<byte> state)
    {
        Position = new Vector2(
            BitConverter.ToSingle(state.Slice(0, 4)),
            BitConverter.ToSingle(state.Slice(4, 4))
        );
    }

    public void Delete()
    {
        QueueFree();
    }

    public ushort NetworkOwnerId { get; set; }
    public void OwnershipChanged(bool isOwned)
    {
        Controlled = isOwned;
    }
}
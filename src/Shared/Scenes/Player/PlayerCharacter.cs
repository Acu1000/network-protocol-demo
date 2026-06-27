using System;
using Godot;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Scenes.Player;

public partial class PlayerCharacter : CharacterBody2D, IEntity
{
	public const float Speed = 6.0f;
	
	public bool Controlled { get; set; } = false;

	public override void _PhysicsProcess(double delta)
	{
		if (Controlled) ProcessControls();
	}

	private void ProcessControls()
	{
		Vector2 velocity = Velocity;
        		
        Vector2 direction = Input.GetVector(
        	"left", 
        	"right", 
        	"up", 
        	"down");
        
        if (direction != Vector2.Zero)
        {
        	velocity = direction * Speed;
        }
        else
        {
        	velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
        	velocity.Y = Mathf.MoveToward(Velocity.Y, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();
	}

	public UInt64? EntityId { get; set; }
	public EntityType EntityType => EntityType.PlayerCharacter;
	public uint LastSnapshotId { get; set; }
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
		Deleted?.Invoke();
		QueueFree();
	}

	public ushort NetworkOwnerId { get; set; }

	public void OwnershipChanged(bool isOwned)
	{
		Controlled = isOwned;
	}

	public void Die()
	{
		Delete();
	}
}
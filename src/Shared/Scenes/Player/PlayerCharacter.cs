using System;
using Godot;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Scenes.Player;

public partial class PlayerCharacter : CharacterBody2D, IEntity
{
	[Export] private float _moveSpeed = 4.0f;
	[Export] private float _turnSpeed = 3.0f;

	[Export] private Node2D _turret;
	
	public bool Controlled { get; set; } = false;

	public override void _PhysicsProcess(double delta)
	{
		if (Controlled) ProcessControls(delta);
	}

	private void ProcessControls(double delta)
	{
		_turret.LookAt(GetGlobalMousePosition());
		
		Vector2 velocity = Velocity;

		float inputMove = Input.GetAxis("up", "down");
		float inputTurn = Input.GetAxis("left", "right");

		Rotation += inputTurn * _turnSpeed * (float)delta;

		velocity = Transform.Y * inputMove * _moveSpeed;

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
		BitConverter.TryWriteBytes(buffer.Slice(0, 4), GlobalPosition.X);
		BitConverter.TryWriteBytes(buffer.Slice(4, 4), GlobalPosition.Y);
		BitConverter.TryWriteBytes(buffer.Slice(8, 4), GlobalRotation);
		BitConverter.TryWriteBytes(buffer.Slice(12, 4), _turret.GlobalRotation);
	}

	public byte[] GetState()
	{
		byte[] buffer = new byte[16];
		WriteStateTo(buffer);
		return buffer;
	}

	public void UpdateState(ReadOnlySpan<byte> state)
	{
		GlobalPosition = new Vector2(
			BitConverter.ToSingle(state.Slice(0, 4)),
			BitConverter.ToSingle(state.Slice(4, 4))
			);
		GlobalRotation = BitConverter.ToSingle(state.Slice(8, 4));
		_turret.GlobalRotation = BitConverter.ToSingle(state.Slice(12, 4));
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
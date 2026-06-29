using System;
using Godot;
using Protocol.Client;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Scenes.Player;

public partial class PlayerCharacter : CharacterBody2D, IEntity
{
	[Export] private float _moveSpeed = 4.0f;
	[Export] private float _turnSpeed = 3.0f;
	[Export] public ushort MaxHealth = 10;
	
	public ushort Health { get; private set; }

	[Export] public Node2D Turret;
	[Export] public Camera2D Camera;
	[Export] public Node2D HealthBarBar;
	[Export] public Node2D HealthBar;
	
	public bool Controlled { get; set; } = false;

	public override void _Ready()
	{
		Health = MaxHealth;

		// For testing speed limit validation
		/*if (!IsServer)
		{
			_moveSpeed *= 3.0f;
		}*/
	}

	public override void _Process(double delta)
	{
		HealthBar.GlobalRotation = 0;
		HealthBarBar.Scale = new Vector2((float)Health / MaxHealth, 1.0f);
		HealthBarBar.Position = new Vector2(0.5f * Health / MaxHealth - 0.5f, 0.0f);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Controlled) ProcessControls(delta);
	}

	private void ProcessControls(double delta)
	{
		Turret.LookAt(GetGlobalMousePosition());
		
		Vector2 velocity = Velocity;

		float inputMove = Input.GetAxis("up", "down");
		float inputTurn = Input.GetAxis("left", "right");

		Rotation += inputTurn * _turnSpeed * (float)delta;

		velocity = Transform.Y * inputMove * _moveSpeed;

        Velocity = velocity;
        MoveAndSlide();

        if (Input.IsActionJustPressed("shoot"))
        {
	        ClientRemoteProcedureManager.Instance?.CallOnServer("shoot", []);
        }
	}

	public void TakeDamage(ushort damage)
	{
		if (Health <= damage)
		{
			Health = 0;
			Die();
		}
		else
		{
			Health -= damage;
			GD.Print("HEALTH = ", Health);
		}
	}
	
	public bool IsServer { get; set; }
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
		BitConverter.TryWriteBytes(buffer.Slice(12, 4), Turret.GlobalRotation);
		BitConverter.TryWriteBytes(buffer.Slice(16, 2), Health);
	}

	public byte[] GetState()
	{
		byte[] buffer = new byte[18];
		WriteStateTo(buffer);
		return buffer;
	}

	public void UpdateState(ReadOnlySpan<byte> state)
	{
		Vector2 NewPos = new Vector2(
			BitConverter.ToSingle(state.Slice(0, 4)),
			BitConverter.ToSingle(state.Slice(4, 4))
			);

		Vector2 PosDiff = NewPos - GlobalPosition;

		if (IsServer && PosDiff.Length() > _moveSpeed * (1.0f / 60.0f) * 1.1f)
		{
			GD.Print("EXCESSIVE SPEED DETECTED");
			GD.Print(PosDiff.Length(), " ", _moveSpeed / 60.0f);
			GD.Print("LIMIT: ", _moveSpeed / 60.0f);
			PosDiff = PosDiff.Normalized() * _moveSpeed / 60.0f;
			GD.Print(PosDiff.Length());
		}
		GlobalPosition += PosDiff;
		
		GlobalRotation = BitConverter.ToSingle(state.Slice(8, 4));
		Turret.GlobalRotation = BitConverter.ToSingle(state.Slice(12, 4));
		if (!IsServer)
		{
			Health = BitConverter.ToUInt16(state.Slice(16, 2));
		}
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
		Camera.Enabled = isOwned;
	}

	public void Die()
	{
		GD.Print("DIED");
		Delete();
	}
}
using Godot;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Scenes.Player;

public partial class PlayerCharacter : CharacterBody2D
{
	public const float Speed = 6.0f;

	public PlayerCharacterEntity Entity { get; set; }

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

        Entity.PositionX = GlobalPosition.X;
        Entity.PositionY = GlobalPosition.Y;
	}
}
using System;
using Godot;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Scenes.Player;

public partial class BasicEnemy : CharacterBody2D
{
    public const float Speed = 1.0f;

    public bool Controlled = false;

    public BasicEnemyEntity Entity;
    
    private Vector2 _velocity;
    
    public override void _PhysicsProcess(double delta)
    {
        if (Controlled) ProcessControlled();
    }

    private void ProcessControlled()
    {
        if (Random.Shared.Next(120) == 1)
        {
            _velocity = new Vector2(
                Random.Shared.NextSingle() * Speed * 2 - Speed,
                Random.Shared.NextSingle() * Speed * 2 - Speed
            ).LimitLength(Speed);
        }

        Velocity = _velocity;
        MoveAndSlide();

        Entity.PositionX = GlobalPosition.X;
        Entity.PositionY = GlobalPosition.Y;
    }
}
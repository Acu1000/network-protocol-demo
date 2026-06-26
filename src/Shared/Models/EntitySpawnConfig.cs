using Godot;
using Protocol.Shared.Entities;

namespace Protocol.Shared.Models;

public class EntitySpawnConfig
{
    public PackedScene Prefab { get; set; }
    public Node? Parent { get; set; }
}
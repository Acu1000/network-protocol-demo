using System;

namespace Protocol.Shared.Entities;

public class PlayerCharacterEntity : Entity
{
    public Single PositionX { get; set; }
    public Single PositionY { get; set; }
    
    public PlayerCharacterEntity() : base(EntityType.PlayerCharacter)
    {
    }

    public override int StateSize => 8;
    public override void WriteStateTo(Span<byte> buffer)
    {
        BitConverter.TryWriteBytes(buffer.Slice(0, 4), PositionX);
        BitConverter.TryWriteBytes(buffer.Slice(4, 4), PositionY);
    }

    public override void UpdateState(ReadOnlySpan<byte> state)
    {
        PositionX = BitConverter.ToSingle(state.Slice(0, 4));
        PositionY = BitConverter.ToSingle(state.Slice(4, 4));
    }
}
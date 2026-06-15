using System;
using Godot;

namespace Protocol.Shared.Entities;

public class SampleEntity : Entity
{
    public Int32 Counter { 
        get => _counter; 
        set => _counter = value;
    }
    
    private Int32 _counter;
    
    public SampleEntity() : base(EntityType.SampleEntity)
    {
    }

    public override int StateSize => 4;

    public override void WriteStateTo(Span<byte> buffer)
    {
        BitConverter.TryWriteBytes(buffer.Slice(0, 4), _counter);
    }

    public override void UpdateState(ReadOnlySpan<byte> state)
    {
        // TODO: validate
        _counter = BitConverter.ToInt32(state.Slice(0, 4));
    }
}
using System;
using System.Runtime.CompilerServices;
using Godot;

namespace Protocol.Shared.Models;

[InlineArray(12)]
public struct Hash12 : IEquatable<Hash12>
{
    private byte _element0;
    
    public bool Equals(Hash12 other)
    {
        ReadOnlySpan<byte> span1 = this;
        ReadOnlySpan<byte> span2 = other;
        
        return span1.SequenceEqual(span2);
    }

    public override string ToString()
    {
        ReadOnlySpan<byte> span = this;
        return BitConverter.ToString(span.ToArray());
    }
}
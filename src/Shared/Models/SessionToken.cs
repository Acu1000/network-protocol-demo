using System;
using System.Runtime.CompilerServices;

namespace Protocol.Shared.Models;

[InlineArray(32)]
public struct SessionToken
{
    private byte _element0;

    public override string ToString()
    {
        ReadOnlySpan<byte> span = this;
        return BitConverter.ToString(span.ToArray());
    }
}
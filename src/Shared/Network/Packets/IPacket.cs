using System;

namespace Protocol.Shared.Network.Packets;

public interface IPacket<T> where T : IPacket<T>
{
    public static abstract bool TryParse(ReadOnlySpan<byte> data, out T packet);
    public void WriteBytesTo(Span<byte> buffer);
    public byte[] ToBytes();
}
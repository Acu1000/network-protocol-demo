using System;
using Godot;
using Protocol.Shared.Util;

namespace Protocol.Shared.Network.Packets;

public struct RemoteProcedureCallPacket : IPacket<RemoteProcedureCallPacket>
{
    public const int PacketMinSize = 1 + 2 + 4;

    public PacketType PacketType => PacketType.RemoteProcedureCall;
    
    public Int32 ProcedureNameHash { get; set; }
    public UInt16 CallerId { get; set; }
    public byte[] Args { get; set; }

    public RemoteProcedureCallPacket(String procedureName, UInt16 callerId, byte[] args)
    {
        ProcedureNameHash = procedureName.Int32Hash();
        CallerId = callerId;
        Args = args;
        
        GD.Print("CREATED ", procedureName, " ", procedureName.Int32Hash());
    }

    public RemoteProcedureCallPacket(Int32 procedureNameHash, UInt16 callerId, byte[] args)
    {
        ProcedureNameHash = procedureNameHash;
        CallerId = callerId;
        Args = args;
    }

    public static bool TryParse(ReadOnlySpan<byte> data, out RemoteProcedureCallPacket packet)
    {
        if (data.Length < PacketMinSize)
        {
            packet = default;
            return false;
        }

        Int32 nameHash = BitConverter.ToInt32(data.Slice(1, 4));
        UInt16 callerId = BitConverter.ToUInt16(data.Slice(5, 2));
        byte[] args = data.Slice(7).ToArray();
        
        packet = new RemoteProcedureCallPacket(nameHash, callerId, args);
        return true;
    }

    public void WriteBytesTo(Span<byte> buffer)
    {
        if (buffer.Length < PacketMinSize + Args.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer));
        }
        
        buffer[0] = (byte)PacketType.RemoteProcedureCall;
        BitConverter.TryWriteBytes(buffer.Slice(1, 4), ProcedureNameHash);
        BitConverter.TryWriteBytes(buffer.Slice(5, 2), CallerId);
        Args.CopyTo(buffer.Slice(7));
    }

    public byte[] ToBytes()
    {
        byte[] buffer = new byte[PacketMinSize + Args.Length];
        WriteBytesTo(buffer);
        return buffer;
    }
}
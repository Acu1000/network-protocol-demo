using System;
using System.Collections.Generic;
using System.Net;
using Godot;
using Protocol.Shared.Network.Packets;
using Protocol.Shared.Util;

namespace Protocol.Shared.Network;

public class RemoteProcedureManager
{
    private readonly Dictionary<Int32, Action<UInt16, ReadOnlySpan<byte>>> _procedures = new();

    public void AddProcedure(String name, Action<UInt16, ReadOnlySpan<byte>> procedure)
    {
        _procedures.Add(name.Int32Hash(), procedure);
        GD.Print("REGISTERED PROCEDURE ", name, " ", name.Int32Hash());
    }

    public void CallProcedureLocal(String name, ReadOnlySpan<byte> arguments)
    {
        throw new NotImplementedException();
        //_procedures.TryGetValue(name.Int32Hash(), out var procedure);
        //procedure?.Invoke(0, arguments);
    }
    
    public void CallProcedureLocal(Int32 hash, UInt16 callerId, ReadOnlySpan<byte> arguments)
    {
        _procedures.TryGetValue(hash, out var procedure);
        procedure?.Invoke(callerId, arguments);
        GD.Print("CALLED ", procedure);
    }

    public void HandleRpcPacket(ReadOnlySpan<byte> packetData, EndPoint endPoint)
    {
        if (!RemoteProcedureCallPacket.TryParse(packetData, out RemoteProcedureCallPacket packet))
        {
            return;
        }
        
        CallProcedureLocal(packet.ProcedureNameHash, packet.CallerId, packet.Args);
    }
}
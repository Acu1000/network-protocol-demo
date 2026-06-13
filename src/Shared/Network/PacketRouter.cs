using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using Godot;
using Protocol.Shared.Network.Packets;

namespace Protocol.Shared.Network;

public class PacketRouter
{
    private readonly Dictionary<PacketType, Action<ReadOnlySpan<byte>, EndPoint>> _packetHandlers = new();
    
    public void Route(ReadOnlySpan<byte> data, EndPoint sourceEndPoint)
    {
        if (data.Length < 1)
        {
            GD.PrintErr("Invalid packet length");
            return;
        }
        
        PacketType packetType = MemoryMarshal.Cast<byte, PacketType>(
            data.Slice(0, 1)
        )[0];
        
        if (_packetHandlers.TryGetValue(packetType, out var handler))
        {
            handler.Invoke(data, sourceEndPoint);
        }
        else
        {
            GD.PrintErr($"Unhandled packet: {packetType.ToString()}");
        }
    }

    public void AddHandler(PacketType packetType, Action<ReadOnlySpan<byte>, EndPoint> handler)
    {
        _packetHandlers.Add(packetType, handler);
    }
}
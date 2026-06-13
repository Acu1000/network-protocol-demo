using System;
using System.Net;
using System.Runtime.InteropServices;
using Godot;
using Protocol.Shared.Network.Models;

namespace Protocol.Client.Network;

public class ClientPacketRouter
{
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
        
        GD.Print(packetType.ToString());

        switch (packetType)
        {
            case PacketType.Ping:
                // TODO: send Pong
                GD.Print("CLIENT: Ping received");
                break;
            
            case PacketType.Pong:
                // TODO: notify session manager or whatever is responsible for this
                break;
        }
    }
}
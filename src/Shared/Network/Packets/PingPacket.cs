using System;
using System.Runtime.InteropServices;
using Protocol.Shared.Network.Models;

namespace Protocol.Shared.Network.Packets;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct PingPacket
{
    public readonly PacketType PacketType = PacketType.Ping;
    public readonly UInt16 TestValue; // Only for testing, not meant to be present in actual production

    public PingPacket(UInt16 testValue)
    {
        TestValue = testValue;
    }

    public PingPacket()
    {
    }
}
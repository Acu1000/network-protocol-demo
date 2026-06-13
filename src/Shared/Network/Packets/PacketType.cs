namespace Protocol.Shared.Network.Models;

public enum PacketType : byte
{
    Ping = 0x05,
    Pong = 0x06,
}
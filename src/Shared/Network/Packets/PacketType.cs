namespace Protocol.Shared.Network.Packets;

public enum PacketType : byte
{
    None = 0x00,
    
    // Session
    ConnectRequest = 0x01,
    Ping = 0x05,
    Pong = 0x06,
    
    // Entity synchronization
    SingleEntityUpdate = 0x11,
    
    // RPC
}
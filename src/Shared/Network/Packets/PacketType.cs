namespace Protocol.Shared.Network.Packets;

public enum PacketType : byte
{
    None = 0x00,
    
    // Session
    ConnectRequest = 0x01,
    ConnectAccept = 0x02,
    DisconnectRequest = 0x03,
    DisconnectAccept = 0x04,
    Ping = 0x05,
    Pong = 0x06,
    
    // Entity synchronization
    SingleEntityUpdate = 0x11,
    
    // RPC
}
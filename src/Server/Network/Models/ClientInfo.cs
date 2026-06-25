using System;
using System.Net;

namespace Protocol.Server.Network.Models;

public class ClientInfo
{
    public string Username { get; set; } = string.Empty;
    public UInt16 ClientId { get; set; }
    public UInt32 SessionToken { get; set; }
    public IPEndPoint EndPoint { get; set; } = default!;
}
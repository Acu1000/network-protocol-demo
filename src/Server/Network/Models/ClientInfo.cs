using System;
using System.Net;
using Protocol.Shared.Models;

namespace Protocol.Server.Network.Models;

public class ClientInfo
{
    public string Username { get; set; } = string.Empty;
    public UInt16 ClientId { get; set; }
    public SessionToken SessionToken { get; set; }
    public IPEndPoint EndPoint { get; set; } = default!;
}
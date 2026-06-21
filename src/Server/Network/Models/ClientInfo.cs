using System;
using System.Net;

namespace Protocol.Server.Network.Models;

public class ClientInfo
{
    public string Username { get; set; } = string.Empty;
    public UInt16 ClientId { get; set; }
    public Int64 SessionToken;
    public EndPoint EndPoint { get; set; } = default!;
}
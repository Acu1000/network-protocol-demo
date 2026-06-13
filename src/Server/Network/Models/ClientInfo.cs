using System;
using System.Net;

namespace Protocol.Server.Network.Models;

public class ClientInfo
{
    public string Username;
    public Int64 SessionToken;
    public EndPoint EndPoint;
}
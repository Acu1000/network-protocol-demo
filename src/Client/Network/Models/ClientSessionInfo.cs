using System;
using System.Net;

namespace Protocol.Client.Network.Models;

public class ClientSessionInfo
{
    public required UInt16 ClientId;
    public required UInt32 SessionToken;
    public required IPEndPoint ServerEndPoint;
}
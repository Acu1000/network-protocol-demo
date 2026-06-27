using System;
using System.Net;
using Protocol.Shared.Models;

namespace Protocol.Client.Network.Models;

public class ClientSessionInfo
{
    public required UInt16 ClientId;
    public required SessionToken SessionToken;
    public required UInt32 SequenceNum;
    public required IPEndPoint ServerEndPoint;
}
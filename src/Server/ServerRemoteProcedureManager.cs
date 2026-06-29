using System;
using Protocol.Server.Network;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Server;

public class ServerRemoteProcedureManager : RemoteProcedureManager
{
    private readonly ServerSessionManager _serverSessionManager;

    public ServerRemoteProcedureManager(ServerSessionManager serverSessionManager)
    {
        _serverSessionManager = serverSessionManager;
    }
    
    public void CallOnAllClients(String name, byte[] args)
    {
        RemoteProcedureCallPacket packet = new(name, 0, args);
        _serverSessionManager.SendToAllClients(packet);
    }
}
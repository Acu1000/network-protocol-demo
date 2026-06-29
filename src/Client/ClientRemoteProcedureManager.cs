using System;
using Godot;
using Protocol.Client.Network;
using Protocol.Shared.Network;
using Protocol.Shared.Network.Packets;

namespace Protocol.Client;

public class ClientRemoteProcedureManager : RemoteProcedureManager
{
    public static ClientRemoteProcedureManager? Instance;
    
    private readonly ClientSessionManager _clientSessionManager;

    public ClientRemoteProcedureManager(ClientSessionManager clientSessionManager)
    {
        Instance = this;
        _clientSessionManager = clientSessionManager;
    }

    public void CallOnServer(String name, byte[] args)
    {
        GD.Print("CLIENT ID IS ", _clientSessionManager.ClientId);
        RemoteProcedureCallPacket packet = new(name, _clientSessionManager.ClientId, args);
        _clientSessionManager.SendToServer(packet);
    }
}
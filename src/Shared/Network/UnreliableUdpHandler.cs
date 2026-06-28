using System;
using System.Net;
using System.Threading.Tasks;

namespace Protocol.Shared.Network;

// For testing
public class UnreliableUdpHandler : UdpHandler
{
    private float _packetLossChance;
    private int _packetDelayMsec;
    
    public UnreliableUdpHandler(int localPort, float packetLossChance, int packetDelayMsec) : base(localPort)
    {
        _packetLossChance = packetLossChance;
        _packetDelayMsec = packetDelayMsec;
    }

    public override void Send(ReadOnlyMemory<byte> data, EndPoint endPoint)
    {
        if (Random.Shared.NextSingle() < _packetLossChance)
        {
            return;
        }

        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(_packetDelayMsec));
            base.Send(data, endPoint);
        });
        
        
    }
}
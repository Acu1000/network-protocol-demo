using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;
using Godot;
using Protocol.Shared.Network.Packets;

namespace Protocol.Shared.Network;

public abstract class BaseUdpHandler
{
    private const int BufferSize = 1024;
    private const int ChannelCapacity = 1024;
    
    private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    
    private readonly Channel<RawPacket> _channel; // For passing data from listener thread to main thread
    protected ChannelReader<RawPacket> Reader => _channel.Reader;

    protected BaseUdpHandler()
    {
        _channel = Channel.CreateBounded<RawPacket>(new BoundedChannelOptions(ChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.DropOldest,
        });
    }

    protected void StartListening(EndPoint endPoint)
    {
        _socket.Bind(endPoint);

        Task.Run(async () => await ListenAsync());
    }
    
    private async Task ListenAsync()
    {
        byte[] buffer = new byte[BufferSize];
        
        while (true)
        {
            try
            {
                EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                var result = await _socket.ReceiveFromAsync(buffer, SocketFlags.None, remoteEp);
                
                byte[] poolBuffer = ArrayPool<byte>.Shared.Rent(result.ReceivedBytes);
                Buffer.BlockCopy(buffer, 0, poolBuffer, 0, result.ReceivedBytes);
                
                _channel.Writer.TryWrite(new RawPacket(result.RemoteEndPoint, poolBuffer, result.ReceivedBytes));
            }
            catch (SocketException ex)
            {
                GD.Print($"Socket Exception: {ex.Message}");
                break;
            }
        }
    }

    protected void Send(ReadOnlyMemory<byte> data, EndPoint endPoint)
    {
        Task.Run(async() => await SendAsync(data, endPoint));
    }
    
    private async Task SendAsync(ReadOnlyMemory<byte> data, EndPoint endPoint)
    {
        await _socket.SendToAsync(data, SocketFlags.None, endPoint);
    }
}
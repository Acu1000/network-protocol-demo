using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;
using Godot;
using Protocol.Shared.Network.Packets;

namespace Protocol.Shared.Network;

public class UdpHandler
{
	private const int BufferSize = 1024;
	private const int ChannelCapacity = 1024;
	
	private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

	private readonly IPEndPoint _localEndPoint;
	
	private readonly Channel<RawPacket> _channel; // For passing data from listener thread to main thread
	protected ChannelReader<RawPacket> Reader => _channel.Reader;

	public UdpHandler(int localPort)
	{
		_localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
		
		_channel = Channel.CreateBounded<RawPacket>(new BoundedChannelOptions(ChannelCapacity)
		{
			SingleReader = true,
			SingleWriter = true,
			FullMode = BoundedChannelFullMode.DropOldest,
		});
	}

	public void StartListening()
	{
		_socket.Bind(_localEndPoint);

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
	
	public void RoutePackets(PacketRouter router)
	{
		while (Reader.TryRead(out RawPacket rawPacket))
		{
			try
			{
				ReadOnlySpan<byte> packetSpan = rawPacket.PoolBuffer.AsSpan(0, rawPacket.NumBytes);
				router.Route(packetSpan, rawPacket.SourceEndPoint);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(rawPacket.PoolBuffer);
			}
		}
	}
	
	public bool TryGetPacket(out byte[] packetSpan, out EndPoint? sourceEndPoint)
	{
		if (!Reader.TryRead(out RawPacket rawPacket))
		{
			packetSpan = default;
			sourceEndPoint = null;
			return false;
		}
		
		try
		{
			// TODO: optimize the whole thing to get rid of array copy
			packetSpan = rawPacket.PoolBuffer[..rawPacket.NumBytes];
			sourceEndPoint = rawPacket.SourceEndPoint;
			return true;
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(rawPacket.PoolBuffer);
		}
	}

	public void Send(ReadOnlyMemory<byte> data, EndPoint endPoint)
	{
		Task.Run(async() => await SendAsync(data, endPoint));
	}
	
	private async Task SendAsync(ReadOnlyMemory<byte> data, EndPoint endPoint)
	{
		await _socket.SendToAsync(data, SocketFlags.None, endPoint);
	}
}

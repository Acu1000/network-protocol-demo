using System;
using System.Linq;
using System.Security.Cryptography;
using Protocol.Shared.Models;

namespace Protocol.Shared.Network.Packets;

public struct ClientPacketHeader
{
    public const int HeaderSize = 2 + 4 + 12;

    public bool IsBlank = false; // Blank header is full zeros, for packets where header is irrelevant
    public readonly UInt16 ClientId;
    public readonly UInt32 SequenceNum;
    public Hash12 Hash;

    public ClientPacketHeader(UInt16 clientId, UInt32 sequenceNum)
    {
        IsBlank = false;
        ClientId = clientId;
        SequenceNum = sequenceNum;
    }
    
    public ClientPacketHeader()
    {
        IsBlank = true;
    }
    
    public static ClientPacketHeader Blank => new ClientPacketHeader();
    
    public static bool TryExtract(ReadOnlySpan<byte> data, out ClientPacketHeader header,
        out ReadOnlySpan<byte> remainingData)
    {
        if (data.Length < HeaderSize)
        {
            header = default;
            remainingData = default;
            return false;
        }

        ReadOnlySpan<byte> zero = stackalloc byte[12];
        if (data.Slice(0, 12).SequenceEqual(zero))
        {
            header = Blank;
            remainingData = data.Slice(HeaderSize);
            return true;
        }

        header = new ClientPacketHeader(
            BitConverter.ToUInt16(data.Slice(0,2)),
            BitConverter.ToUInt32(data.Slice(2, 4))
            );
        data.Slice(6, 12).CopyTo(header.Hash);
        
        remainingData = data.Slice(HeaderSize);
        return true;
    }

    public void WriteBytesTo(Span<byte> buffer)
    {
        if (buffer.Length < HeaderSize)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer));
        }
        
        if (IsBlank)
        {
            buffer.Slice(0, 12).Fill(0);
            return;
        }
        
        BitConverter.TryWriteBytes(buffer.Slice(0, 2), ClientId);
        BitConverter.TryWriteBytes(buffer.Slice(2, 4), SequenceNum);
        Span<byte> hashSpan = Hash;
        hashSpan.CopyTo(buffer.Slice(6, 12));
    }
    
    public byte[] ToBytes()
    {
        byte[] bytes = new byte[HeaderSize];
        WriteBytesTo(bytes);
        return bytes;
    }
    
    // TODO: optimize to get rid of array copy
    public byte[] AppendPacket(SessionToken sessionToken, ReadOnlySpan<byte> data)
    {
        Hash = ComputeHash(sessionToken, data);
        return [.. ToBytes(), .. data];
    }

    // Appends packet to a blank header, used for ConnectRequest which ignores the header
    public static byte[] AppendPacketToBlank(ReadOnlySpan<byte> data)
    {
        return [..Blank.ToBytes(), .. data];
    }

    public Hash12 ComputeHash(SessionToken sessionToken, ReadOnlySpan<byte> payload)
    {
        using (IncrementalHash hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, sessionToken))
        {
            Span<byte> sequenceNumBytes = stackalloc byte[sizeof(UInt32)];
            BitConverter.TryWriteBytes(sequenceNumBytes, SequenceNum);
            
            Span<byte> clientIdBytes = stackalloc byte[sizeof(UInt16)];
            BitConverter.TryWriteBytes(clientIdBytes, ClientId);
            
            hmac.AppendData(sequenceNumBytes);
            hmac.AppendData(payload);

            Span<byte> fullHash = stackalloc byte[32];
            hmac.TryGetCurrentHash(fullHash, out int bytesWritten);
            if (bytesWritten < 12)
            {
                throw new Exception("HASH ERROR");
            }

            // truncate hash to be 12 bytes
            Hash12 hash = new();
            fullHash.Slice(0, 12).CopyTo(hash);
            return hash;
        }
    }
}
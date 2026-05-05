using Basalt.Core;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.RakNet;
using Basalt.RakNet.Packets.Enums;
using System.Buffers;
using System.IO.Compression;
using Basalt.Network.Handlers;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using BinaryReader = Basalt.Binary.BinaryReader;

namespace Basalt.Network;

public sealed class NetworkHandler
{
    private readonly Server _server;

    public NetworkHandler(Server server) => _server = server;

    public void HandlePacket(NetworkConnection connection, ReadOnlyMemory<byte> payload)
    {
        ReadOnlySpan<byte> span = payload.Span;
        if (span.Length == 0 || span[0] != 0xFE) return;
        
        span = span[1..];
        if (span.Length == 0) return;

        CompressionMethod method = (CompressionMethod)span[0];
        if (method == CompressionMethod.Zlib || method == CompressionMethod.Snappy || method == CompressionMethod.None)
            span = span[1..];
        else
            method = CompressionMethod.NotPresent;

        byte[]? decompressed = null;
        try
        {
            ReadOnlySpan<byte> framed;
            if (method == CompressionMethod.Zlib)
            {
                decompressed = ArrayPool<byte>.Shared.Rent(1024 * 1024);
                int size = Decompress(span, decompressed);
                framed = decompressed.AsSpan(0, size);
            }
            else
            {
                framed = span;
            }

            if (framed.Length == 0) return;

            BinaryReader reader = new(framed);
            while (reader.Remaining > 0)
            {
                int length = reader.ReadVarInt();
                if (length <= 0 || length > reader.Remaining) break;

                ReadOnlySpan<byte> buffer = reader.ReadBytes(length);
                if (buffer.Length == 0) continue;

                PacketId id = (PacketId)new BinaryReader(buffer).ReadVarInt();
                switch (id)
                {
                    case PacketId.RequestNetworkSettings:
                        RequestNetworkSettings.Handle(_server, connection, buffer);
                        break;
                    default:
                        Console.WriteLine($"Unhandled 0x{(byte)id:X2} ({buffer.Length} bytes)");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Network error: {ex.Message}");
        }
        finally
        {
            if (decompressed != null) ArrayPool<byte>.Shared.Return(decompressed);
        }
    }

    private static int Decompress(ReadOnlySpan<byte> input, byte[] output)
    {
        byte[] temp = ArrayPool<byte>.Shared.Rent(input.Length);
        try
        {
            input.CopyTo(temp);
            using var ms = new MemoryStream(temp, 0, input.Length);
            using var deflate = new DeflateStream(ms, CompressionMode.Decompress);
            
            int total = 0, read;
            while ((read = deflate.Read(output, total, output.Length - total)) > 0)
                total += read;
            return total;
        }
        finally { ArrayPool<byte>.Shared.Return(temp); }
    }

    public void SendPacket(NetworkConnection connection, DataPacket packet, CompressionMethod? method = null)
    {
        SendPackets(connection, [packet], method);
    }

    public void SendPackets(NetworkConnection connection, IEnumerable<DataPacket> pks, CompressionMethod? method = null)
    {
        byte[] frameBuffer = ArrayPool<byte>.Shared.Rent(1024 * 512);
        byte[] pkgBuffer = ArrayPool<byte>.Shared.Rent(1024 * 64);

        try
        {
            BinaryWriter writer = new(frameBuffer);
            foreach (var packet in pks)
            {
                BinaryWriter pkgWriter = new(pkgBuffer);
                pkgWriter.WriteVarInt((int)packet.PacketId);
                packet.Serialize(ref pkgWriter);
                
                ReadOnlySpan<byte> data = pkgWriter.GetBuffer();
                writer.WriteVarInt(data.Length);
                writer.WriteBytes(data);
            }

            ReadOnlySpan<byte> frame = writer.GetBuffer();
            CompressionMethod m = method ?? _server.Options.CompressionMethod;

            if (m != CompressionMethod.None && frame.Length < _server.Options.CompressionThreshold)
                m = CompressionMethod.None;

            if (m == CompressionMethod.Zlib)
            {
                byte[] compressed = ArrayPool<byte>.Shared.Rent(frame.Length + 256);
                try
                {
                    int size = Compress(frame, compressed);
                    SendFramed(connection, compressed.AsSpan(0, size), m);
                }
                finally { ArrayPool<byte>.Shared.Return(compressed); }
            }
            else
            {
                SendFramed(connection, frame, m);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(frameBuffer);
            ArrayPool<byte>.Shared.Return(pkgBuffer);
        }
    }

    private static int Compress(ReadOnlySpan<byte> input, byte[] output)
    {
        using var ms = new MemoryStream(output);
        using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, true))
            deflate.Write(input);
        return (int)ms.Position;
    }

    private static void SendFramed(NetworkConnection connection, ReadOnlySpan<byte> payload, CompressionMethod method)
    {
        int headerSize = (method == CompressionMethod.NotPresent) ? 1 : 2;
        byte[] final = ArrayPool<byte>.Shared.Rent(payload.Length + headerSize);
        try
        {
            final[0] = 0xFE;
            if (method != CompressionMethod.NotPresent) final[1] = (byte)method;
            payload.CopyTo(final.AsSpan(headerSize));
            connection.SendPacket(final.AsSpan(0, payload.Length + headerSize), Reliability.ReliableOrdered);
        }
        finally { ArrayPool<byte>.Shared.Return(final); }
    }
}


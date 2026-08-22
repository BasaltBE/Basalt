namespace Basalt.Core.Network;

using System.IO.Compression;
using ConMaster.Compression;
using Basalt.Binary;

using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

internal static class PacketCompression {
    public static int Compress(ReadOnlySpan<byte> input, Span<byte> output, CompressionMethod compression) {
        if (compression == CompressionMethod.Snappy) {
            throw new NotSupportedException("Snappy compression is not supported.");
        }

        int headerSize = compression == CompressionMethod.NotPresent ? 0 : 1;
        if (output.Length < input.Length + headerSize) {
            throw new ArgumentException("Output buffer is too small.", nameof(output));
        }

        int outputOffset = 0;
        if (compression != CompressionMethod.NotPresent) {
            output[0] = (byte)compression;
            outputOffset = 1;
        }

        if (compression == CompressionMethod.Zlib) {
            DeflateCompressor compressor = new() {
                CompressionLevel = CompressionLevel.Fastest,
            };
            compressor.Compress(input, output[outputOffset..], out int bytesWritten);
            return bytesWritten + outputOffset;
        }

        input.CopyTo(output[outputOffset..]);
        return input.Length + outputOffset;
    }

    public static int Decompress(ReadOnlySpan<byte> input, Span<byte> output) {
        DeflateCompressor decompressor = new();
        decompressor.Decompress(input, output, out int bytesWritten);
        return bytesWritten;
    }

    public static int Frame(ReadOnlySpan<byte> input, Span<byte> output, CompressionMethod compression, int compressionThreshold) {
        CompressionMethod method = compression;
        if (method != CompressionMethod.None && method != CompressionMethod.NotPresent && input.Length < compressionThreshold) {
            method = CompressionMethod.None;
        }

        if (output.Length == 0) {
            throw new ArgumentException("Output buffer is too small.", nameof(output));
        }

        output[0] = 0xFE;
        return Compress(input, output[1..], method) + 1;
    }

    public static int GetFrameCapacity(int inputLength, CompressionMethod compression) {
        int payloadCapacity = compression == CompressionMethod.Zlib
            ? checked(inputLength + (inputLength >> 12) + (inputLength >> 14) + (inputLength >> 25) + 13)
            : inputLength;
        int headerSize = compression == CompressionMethod.NotPresent ? 1 : 2;
        return checked(payloadCapacity + headerSize);
    }

    public static int Frame(ReadOnlySpan<ReadOnlyMemory<byte>> packets, Span<byte> output) {
        int offset = 0;
        BinaryWriter writer = new(output, ref offset);
        foreach (ReadOnlyMemory<byte> packet in packets) {
            writer.WriteVarUInt((uint)packet.Length);
            writer.WriteBytes(packet.Span);
        }
        return offset;
    }

    public static int Unframe(ReadOnlySpan<byte> input, Span<byte> output, out CompressionMethod compression) {
        if (input.Length == 0 || input[0] != 0xFE) {
            compression = CompressionMethod.NotPresent;
            return 0;
        }

        ReadOnlySpan<byte> payload = input[1..];
        if (payload.Length == 0) {
            compression = CompressionMethod.NotPresent;
            return 0;
        }

        CompressionMethod header = (CompressionMethod)payload[0];
        switch (header) {
            case CompressionMethod.Zlib:
                compression = CompressionMethod.Zlib;
                return Decompress(payload[1..], output);
            case CompressionMethod.None:
                compression = CompressionMethod.None;
                payload[1..].CopyTo(output);
                return payload.Length - 1;
            case CompressionMethod.Snappy:
                throw new NotSupportedException("Snappy decompression is not supported.");
            default:
                compression = CompressionMethod.NotPresent;
                payload.CopyTo(output);
                return payload.Length;
        }
    }

    public static ReadOnlyMemory<byte>[] Unframe(ReadOnlyMemory<byte> frame) {
        ReadOnlySpan<byte> span = frame.Span;
        int offset = 0;
        BinaryReader reader = new(span, ref offset);
        List<ReadOnlyMemory<byte>> packets = [];
        while (reader.Remaining > 0) {
            int packetLength = checked((int)reader.ReadVarUInt());
            if (packetLength <= 0 || packetLength > reader.Remaining) {
                break;
            }

            int packetOffset = reader.Offset;
            reader.Advance(packetLength);
            packets.Add(frame.Slice(packetOffset, packetLength));
        }
        return [.. packets];
    }
}

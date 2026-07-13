// This is an IO style protocol, which is supposed to be a friendly
// interface for users and developers. 
// Slightly inspired by Zig's 0.16.0 std.Io

// using System.Runtime.Serialization;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Enums;
using System.Reflection;
using System.IO.Compression;
using ConMaster.Compression;
using Basalt.Binary;

using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Io
{
    public static class Constants
    {
        public const int ProtocolVersion = 1001;
        public const string MinecraftVersion = "1.26.30";
        public const int ShieldNetworkId = 355;
    }
 
    /// <summary>
    /// Methods for processing packets
    /// </summary>
    public static class Packet
    {
        static readonly IReadOnlyDictionary<PacketId, Func<DataPacket>> Pool;
        static readonly IReadOnlyDictionary<Type, PacketId> TypeIds;


        /// <summary>
        /// A pool of packets
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        static Packet()
        {
            Dictionary<PacketId, Func<DataPacket>> pool = [];
            Dictionary<Type, PacketId> typeIds = [];

            foreach (Type type in typeof(DataPacket).Assembly.GetTypes())
            {
                if (!typeof(DataPacket).IsAssignableFrom(type) || type.IsAbstract)
                {
                    continue;
                }

                PacketAttribute? attribute = type.GetCustomAttribute<PacketAttribute>();
                if (attribute is null)
                {
                    continue;
                }

                if (pool.ContainsKey(attribute.Id))
                {
                    throw new InvalidOperationException($"Duplicate packet id mapping for {attribute.Id}.");
                }

                pool[attribute.Id] = () =>
                {
                    object? instance = Activator.CreateInstance(type);
                    if (instance is not DataPacket packet)
                    {
                        throw new InvalidOperationException($"{type.FullName} could not be created.");
                    }

                    return packet;
                };
                typeIds[type] = attribute.Id;
            }

            Pool = pool;
            TypeIds = typeIds;
        }

        /// <summary>
        /// Deserialized a packet by just providing a reader,
        /// each packet class already includes a PacketID, so when you deserialize it
        /// you can just use DataPacket.PacketId to check what packet it is
        /// </summary>
        public static DataPacket Deserialize(BinaryReader reader)
        {
            PacketId id = (PacketId)reader.ReadVarUInt();

            if (!Pool.TryGetValue(id, out Func<DataPacket>? create))
            {
                throw new NotImplementedException($"Deserialization for packet ID {(byte)id} ({id}) is not implemented.");
            }

            DataPacket packet = create();
            packet.Deserialize(reader);
            return packet;
        }

        public static PacketId GetId(DataPacket packet)
        {
            Type type = packet.GetType();
            if (!TypeIds.TryGetValue(type, out PacketId id))
            {
                throw new NotImplementedException($"Packet id for {type.FullName} is not implemented.");
            }

            return id;
        }

        public static void Serialize(DataPacket packet, BinaryWriter writer)
        {
            writer.WriteVarUInt((uint)GetId(packet));
            packet.Serialize(writer);
        }

        public static int Compress(ReadOnlySpan<byte> input, Span<byte> output, CompressionMethod compression)
        {
            if (compression == CompressionMethod.Snappy)
            {
                throw new NotSupportedException("Snappy compression is not supported.");
            }

            int headerSize = compression == CompressionMethod.NotPresent ? 0 : 1;
            if (output.Length < input.Length + headerSize)
            {
                throw new ArgumentException("Output buffer is too small.", nameof(output));
            }

            int outputOffset = 0;
            if (compression != CompressionMethod.NotPresent)
            {
                output[0] = (byte)compression;
                outputOffset = 1;
            }

            if (compression == CompressionMethod.Zlib)
            {
                DeflateCompressor compressor = new()
                {
                    CompressionLevel = CompressionLevel.Fastest
                };

                compressor.Compress(input, output[outputOffset..], out int bytesWritten);
                return bytesWritten + outputOffset;
            }

            input.CopyTo(output[outputOffset..]);
            return input.Length + outputOffset;
        }

        public static int Decompress(ReadOnlySpan<byte> input, Span<byte> output)
        {
            DeflateCompressor compressor = new();
            compressor.Decompress(input, output, out int bytesWritten);
            return bytesWritten;
        }

        public static int Compress(ReadOnlySpan<byte> input, Span<byte> output)
        {
            DeflateCompressor compressor = new()
            {
                CompressionLevel = CompressionLevel.Fastest
            };

            compressor.Compress(input, output, out int bytesWritten);
            return bytesWritten;
        }

        public static int Frame(ReadOnlySpan<byte> input, Span<byte> output, CompressionMethod compression, int compressionThreshold)
        {
            CompressionMethod method = compression;

            if (method != CompressionMethod.None && method != CompressionMethod.NotPresent && input.Length < compressionThreshold)
            {
                method = CompressionMethod.None;
            }

            if (output.Length == 0)
            {
                throw new ArgumentException("Output buffer is too small.", nameof(output));
            }

            output[0] = 0xFE;
            int bytesWritten = Compress(input, output[1..], method);
            return bytesWritten + 1;
        }

        public static int Frame(ReadOnlyMemory<byte>[] packets, Span<byte> output)
        {
            int offset = 0;
            BinaryWriter writer = new(output, ref offset);

            foreach (ReadOnlyMemory<byte> packet in packets)
            {
                writer.WriteVarInt(packet.Length);
                writer.WriteBytes(packet.Span);
            }

            return offset;
        }

        public static int Unframe(ReadOnlySpan<byte> input, Span<byte> output, out CompressionMethod compression)
        {
            if (input.Length == 0 || input[0] != 0xFE)
            {
                compression = CompressionMethod.NotPresent;
                return 0;
            }

            ReadOnlySpan<byte> payload = input[1..];
            if (payload.Length == 0)
            {
                compression = CompressionMethod.NotPresent;
                return 0;
            }

            CompressionMethod header = (CompressionMethod)payload[0];

            switch (header)
            {
                case CompressionMethod.Zlib:
                    compression = CompressionMethod.Zlib;
                    {
                        DeflateCompressor compressor = new();
                        compressor.Decompress(payload[1..], output, out int bytesWritten);
                        return bytesWritten;
                    }

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

        /// <summary>
        /// Unframes a packet.
        /// But this one allocates memory for arrays, so use it visely!
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static ReadOnlyMemory<byte>[] Unframe(ReadOnlyMemory<byte> frame)
        {
            ReadOnlySpan<byte> span = frame.Span;
            int offset = 0;
            BinaryReader reader = new(span, ref offset);
            List<ReadOnlyMemory<byte>> packets = [];

            while (reader.Remaining > 0)
            {
                int packetLength = checked((int)reader.ReadVarUInt());
                if (packetLength <= 0 || packetLength > reader.Remaining)
                {
                    break;
                }

                int packetOffset = reader.Offset;
                reader.Advance(packetLength);
                packets.Add(frame.Slice(packetOffset, packetLength));
            }

            return [..packets];
        }
    }
}

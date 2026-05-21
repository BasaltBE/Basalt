// This is an IO style protocol, which is supposed to be a friendly
// interface for users and developers. 
// Slightly inspired by Zig's 0.16.0 std.Io

// using System.Runtime.Serialization;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Enums;
using System.Reflection;

using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Io
{
    public static class Constants
    {
        public const int ProtocolVersion = 975;
        public const string MinecraftVersion = "1.26.21";
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
    }
}

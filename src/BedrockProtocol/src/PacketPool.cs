using System.Reflection;
using System.Linq.Expressions;
namespace Basalt.BedrockProtocol;

public static class PacketPool {
    private static readonly Dictionary<int, Type> PacketTypes = CreatePacketTypes();
    private static readonly Dictionary<int, Func<DataPacket>> PacketFactories = CreatePacketFactories();

    public static bool TryGetPacketType(int id, out Type? packetType) {
        return PacketTypes.TryGetValue(id, out packetType);
    }

    public static Type GetPacketType(int id) {
        if (!PacketTypes.TryGetValue(id, out Type? packetType)) {
            throw new KeyNotFoundException($"No packet is registered for ID {id}.");
        }

        return packetType;
    }

    public static DataPacket Create(int id) {
        return PacketFactories[id]();
    }

    public static bool TryCreate(int id, out DataPacket? packet) {
        if (!PacketFactories.TryGetValue(id, out Func<DataPacket>? factory)) {
            packet = null;
            return false;
        }

        packet = factory();
        return true;
    }

    private static Dictionary<int, Type> CreatePacketTypes() {
        var packetTypes = new Dictionary<int, Type>();

        foreach (Type type in typeof(DataPacket).Assembly.GetTypes()) {
            if (type.IsAbstract || !typeof(DataPacket).IsAssignableFrom(type)) {
                continue;
            }

            PacketIdAttribute? metadata = type.GetCustomAttribute<PacketIdAttribute>();
            if (metadata is null) {
                continue;
            }

            if (!packetTypes.TryAdd(metadata.Id, type)) {
                throw new InvalidOperationException($"Packet ID {metadata.Id} is already registered.");
            }
        }

        return packetTypes;
    }

    private static Dictionary<int, Func<DataPacket>> CreatePacketFactories() {
        var factories = new Dictionary<int, Func<DataPacket>>(PacketTypes.Count);
        foreach ((int id, Type type) in PacketTypes) {
            factories[id] = Expression.Lambda<Func<DataPacket>>(Expression.New(type)).Compile();
        }

        return factories;
    }
}

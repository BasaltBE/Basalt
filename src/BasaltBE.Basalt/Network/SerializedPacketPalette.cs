using System.Collections.Concurrent;
using BasaltBE.Basalt.Crafting;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Items;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network;

public sealed class SerializedPacketPalette(NetworkServer network) {
    private readonly ConcurrentDictionary<Type, byte[]> packets = [];

    public void Serialize<T>(T packet) where T : DataPacket {
        ArgumentNullException.ThrowIfNull(packet);
        packets[typeof(T)] = network.PreparePacket(packet);
    }

    public ReadOnlyMemory<byte> Get<T>() where T : DataPacket {
        if (packets.TryGetValue(typeof(T), out byte[]? packet))
            return packet;

        throw new KeyNotFoundException($"No serialized packet exists for {typeof(T).Name}.");
    }

    public void SerializeDefault() {
        Serialize(ItemPalette.RegistryPacket);
        Serialize(ItemPalette.CreativeContentPacket);
        Serialize(CraftingPalette.Packet);

        CompoundTag actorIdentifiers = new();
        ListTag identifierList = new();
        foreach (EntityType type in EntityPalette.Types) {
            CompoundTag identifier = new();
            identifier.Add("bid", new StringTag { Value = string.Empty });
            identifier.Add("id", new StringTag { Value = type.Identifier });
            identifierList.Value.Add(identifier);
        }
        actorIdentifiers.Add("idlist", identifierList);
        Serialize(new AvailableActorIdentifiersPacket { IdentifierList = actorIdentifiers });
    }
}

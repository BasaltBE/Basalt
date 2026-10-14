using System.Reflection;
using Basalt.Binary;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Items;

public static class ItemPalette {
    public static readonly ItemType[] Types = LoadTypes();
    static readonly Dictionary<string, ItemType> ByIdentifier = Types.ToDictionary(type => type.Identifier, StringComparer.Ordinal);
    static readonly Dictionary<int, ItemType> ByNetworkId = Types.ToDictionary(type => type.NetworkId);
    internal static readonly ItemRegistryPacket RegistryPacket = CreateRegistryPacket();
    internal static readonly byte[] SerializedRegistry = SerializeRegistry(RegistryPacket);

    public static ItemType? Find(string identifier) => ByIdentifier.GetValueOrDefault(identifier);

    public static ItemType? FindByNetworkId(int networkId) => ByNetworkId.GetValueOrDefault(networkId);

    static ItemRegistryPacket CreateRegistryPacket() {
        ItemData[] items = new ItemData[Types.Length];
        for (int i = 0; i < Types.Length; i++) {
            ItemType type = Types[i];
            items[i] = new ItemData {
                Name = type.Identifier,
                Id = checked((short)type.NetworkId),
                ComponentBased = type.ComponentBased,
                Version = type.Version,
                ComponentData = type.PropertiesPayload.ToComponentData()
            };
        }

        return new ItemRegistryPacket { Items = items };
    }

    static byte[] SerializeRegistry(ItemRegistryPacket packet) {
        using BinaryStream stream = BinaryStream.Rent(2 * 1024 * 1024);
        BinaryWriter writer = stream.GetWriter();
        packet.Serialize(ref writer);
        return stream.Buffer.Span[..stream.Offset].ToArray();
    }

    static ItemType[] LoadTypes() {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = dataAssembly.GetManifestResourceStream("BasaltBE.Data.Data.item-types.json")
            ?? throw new InvalidOperationException("Item data file 'item-types.json' is missing.");

        return ItemTypeParser.Parse(stream);
    }
}

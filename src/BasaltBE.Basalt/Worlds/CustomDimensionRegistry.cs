using System.Buffers.Binary;
using BasaltBE.Basalt.Worlds.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Worlds;

public sealed class CustomDimensionRegistry(Server server) {
    private readonly Dictionary<string, CustomDimensionDefinition> definitions = new(StringComparer.Ordinal);
    private int nextRuntimeId = 1000;

    public int Count => definitions.Count;

    public CustomDimensionDefinition Register(
        string identifier,
        Guid packId,
        int minimumY = -64,
        int heightRange = 384,
        GeneratorType generatorType = GeneratorType.Void,
        string defaultBiome = "minecraft:ocean",
        int cloudHeight = 128,
        bool renderClouds = false
    ) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultBiome);
        if (packId == Guid.Empty)
            throw new ArgumentException("Custom dimensions require the UUID of their registering pack.", nameof(packId));
        if (server.Started)
            throw new InvalidOperationException("Custom dimensions must be registered before the server starts.");
        if (minimumY != -64 || heightRange != 384)
            throw new ArgumentOutOfRangeException(nameof(heightRange), "Custom dimensions must use the current world height of -64 to 319.");
        if (!Enum.IsDefined(generatorType))
            throw new ArgumentOutOfRangeException(nameof(generatorType));
        if (definitions.ContainsKey(identifier))
            throw new InvalidOperationException($"Custom dimension '{identifier}' is already registered.");

        CustomDimensionDefinition definition = new(
            identifier,
            minimumY,
            heightRange,
            generatorType,
            defaultBiome,
            cloudHeight,
            renderClouds,
            packId,
            nextRuntimeId++
        );
        definitions.Add(identifier, definition);
        return definition;
    }

    internal bool Contains(CustomDimensionDefinition definition) =>
        definitions.TryGetValue(definition.Identifier, out CustomDimensionDefinition? registered) && ReferenceEquals(definition, registered);

    internal DimensionDataPacket CreatePacket() {
        DimensionDataPacket packet = new();
        Span<byte> packId = stackalloc byte[16];
        foreach ((string identifier, CustomDimensionDefinition definition) in definitions) {
            definition.PackId.TryWriteBytes(packId);
            packet.Definitions.Add(identifier, new DimensionDefinition {
                MinimumY = definition.MinimumY,
                HeightRange = definition.HeightRange,
                GeneratorType = definition.GeneratorType,
                RuntimeId = definition.RuntimeId,
                PackId = new Uuid {
                    MostSignificantBits = BinaryPrimitives.ReadUInt64LittleEndian(packId[..8]),
                    LeastSignificantBits = BinaryPrimitives.ReadUInt64LittleEndian(packId[8..])
                },
                DefaultBiome = definition.DefaultBiome,
                CloudHeight = definition.CloudHeight,
                RenderClouds = definition.RenderClouds
            });
        }

        return packet;
    }
}

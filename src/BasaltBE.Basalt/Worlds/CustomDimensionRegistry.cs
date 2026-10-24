using System.Buffers.Binary;
using System.Reflection;
using BasaltBE.Basalt.Worlds.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;

namespace BasaltBE.Basalt.Worlds;

public sealed class CustomDimensionRegistry(Server server) {
    const int FirstCustomBiomeId = 30000;
    static readonly Lazy<BiomeDefinitionListPacket> BuiltInBiomes = new(LoadBiomeDefinitions);
    private readonly Dictionary<string, CustomDimensionDefinition> definitions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BiomeDefinitionData> biomes = new(StringComparer.Ordinal);
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
        if (minimumY is < -512 or > 512 || (minimumY & 15) != 0)
            throw new ArgumentOutOfRangeException(nameof(minimumY), "Custom dimension minimum Y must be within -512 to 512 and align to a 16-block section.");
        if (heightRange is < 16 or > 1024 || (heightRange & 15) != 0 || minimumY + heightRange > 512)
            throw new ArgumentOutOfRangeException(nameof(heightRange), "Custom dimension height must be from 16 to 1024, align to a 16-block section, and end at or below Y 512.");
        if (!Enum.IsDefined(generatorType))
            throw new ArgumentOutOfRangeException(nameof(generatorType));
        if (definitions.ContainsKey(identifier))
            throw new InvalidOperationException($"Custom dimension '{identifier}' is already registered.");

        BiomeDefinitionListPacket builtinBiomes = BuiltInBiomes.Value;
        int defaultBiomeId = 0;
        if (defaultBiome.StartsWith("minecraft:", StringComparison.Ordinal)) {
            int biomeIndex = Array.IndexOf(builtinBiomes.Strings, defaultBiome);
            if (biomeIndex >= 0 && builtinBiomes.Definitions.TryGetValue(checked((ushort)biomeIndex), out BiomeDefinitionData? builtinBiome))
                defaultBiomeId = builtinBiome.Id;
            else if (defaultBiome != "minecraft:ocean")
                throw new ArgumentException($"Unknown built-in biome '{defaultBiome}'.", nameof(defaultBiome));
        }
        else if (!biomes.TryGetValue(defaultBiome, out BiomeDefinitionData? customBiome)) {
            int biomeId = checked(FirstCustomBiomeId + biomes.Count);
            if (biomeId > ushort.MaxValue || builtinBiomes.Strings.Length + biomes.Count > ushort.MaxValue)
                throw new InvalidOperationException("The custom biome registry is full.");
            customBiome = new BiomeDefinitionData {
                Id = checked((ushort)biomeId),
                Temperature = 0.5f,
                Depth = 0.1f,
                Scale = 0.2f,
                MapWaterColorArgb = unchecked((int)0xA660B7FF),
                Tags = [88]
            };
            biomes.Add(defaultBiome, customBiome);
            defaultBiomeId = biomeId;
        }
        else {
            defaultBiomeId = customBiome.Id;
        }

        CustomDimensionDefinition definition = new(
            identifier,
            minimumY,
            heightRange,
            generatorType,
            defaultBiome,
            defaultBiomeId,
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

    internal BiomeDefinitionListPacket CreateBiomePacket() {
        BiomeDefinitionListPacket builtinBiomes = BuiltInBiomes.Value;
        Dictionary<ushort, BiomeDefinitionData> definitions = new(builtinBiomes.Definitions);
        string[] strings = new string[builtinBiomes.Strings.Length + biomes.Count];
        builtinBiomes.Strings.CopyTo(strings, 0);
        int index = builtinBiomes.Strings.Length;
        foreach ((string identifier, BiomeDefinitionData definition) in biomes) {
            definitions.Add(checked((ushort)index), definition);
            strings[index++] = identifier;
        }

        return new BiomeDefinitionListPacket {
            Definitions = definitions,
            Strings = strings
        };
    }

    static BiomeDefinitionListPacket LoadBiomeDefinitions() {
        Assembly assembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = assembly.GetManifestResourceStream("BasaltBE.Data.Data.biome-definitions.bin")
            ?? throw new InvalidDataException("Vanilla biome definitions were not found.");
        using MemoryStream memory = new();
        stream.CopyTo(memory);
        byte[] payload = memory.ToArray();
        int offset = 0;
        BinaryReader reader = new(payload, ref offset);
        BiomeDefinitionListPacket packet = new();
        packet.Deserialize(ref reader);
        if (reader.Remaining != 0)
            throw new InvalidDataException("Vanilla biome definitions contain trailing data.");
        return packet;
    }
}

using Basalt.Binary;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Providers;
using BasaltBE.Logging;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using GameRule = BasaltBE.Basalt.Worlds.Types.GameRule;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Worlds;

public sealed class GameRules {
    private readonly World world;
    private readonly CompoundTag worldData = new();
    private bool keepInventory;
    private bool showCoordinates;
    private bool fallDamage = true;
    private bool tntExplodes = true;

    internal GameRules(World world, byte[]? data) {
        this.world = world;
        if (data is null)
            return;

        try {
            int offset = 0;
            if (BasaltBE.Nbt.Nbt.Read(data, ref offset) is not CompoundTag savedWorldData || offset != data.Length)
                throw new InvalidDataException("World data has an invalid NBT format.");

            if (savedWorldData.Get<IntTag>("DataVersion")?.Value == 1) {
                worldData.Add("keepinventory", savedWorldData.Get<ByteTag>("keepinventory") ?? new ByteTag());
                worldData.Add("showcoordinates", savedWorldData.Get<ByteTag>("showcoordinates") ?? new ByteTag());
                worldData.Add("falldamage", savedWorldData.Get<ByteTag>("falldamage") ?? new ByteTag { Value = 1 });
                worldData.Add("tntexplodes", savedWorldData.Get<ByteTag>("tntexplodes") ?? new ByteTag { Value = 1 });
                if (savedWorldData.Get<LongTag>("Time") is { } savedTime)
                    worldData.Add("Time", savedTime);
                if (savedWorldData.Get<LongTag>("currentTick") is { } savedCurrentTick)
                    worldData.Add("currentTick", savedCurrentTick);
            } else {
                foreach ((string name, Tag tag) in savedWorldData.Tags)
                    worldData.Add(name, tag);
            }

            world.GameTime = worldData.Get<LongTag>("Time")?.Value ?? worldData.Get<LongTag>("currentTick")?.Value ?? 0;
            if (worldData.Get<ByteTag>("keepinventory") is { } keepInventoryTag)
                keepInventory = keepInventoryTag.Value != 0;
            if (worldData.Get<ByteTag>("showcoordinates") is { } showCoordinatesTag)
                showCoordinates = showCoordinatesTag.Value != 0;
            if (worldData.Get<ByteTag>("falldamage") is { } fallDamageTag)
                fallDamage = fallDamageTag.Value != 0;
            if (worldData.Get<ByteTag>("tntexplodes") is { } tntExplodesTag)
                tntExplodes = tntExplodesTag.Value != 0;
        }
        catch (Exception error) when (error is IOException or ArgumentException or IndexOutOfRangeException or OverflowException) {
            Logger.Scoped("Storage").Warn($"Could not load game rules for world {world.Identifier}: {error.Message}");
        }
    }

    public bool Get(GameRule rule) => rule switch {
        GameRule.KeepInventory => keepInventory,
        GameRule.ShowCoordinates => showCoordinates,
        GameRule.FallDamage => fallDamage,
        GameRule.TntExplodes => tntExplodes,
        _ => throw new ArgumentOutOfRangeException(nameof(rule))
    };

    public bool Set(GameRule rule, bool value) {
        world.CheckOwnership();
        if (Get(rule) == value)
            return false;

        switch (rule) {
            case GameRule.KeepInventory:
                keepInventory = value;
                break;
            case GameRule.ShowCoordinates:
                showCoordinates = value;
                break;
            case GameRule.FallDamage:
                fallDamage = value;
                break;
            case GameRule.TntExplodes:
                tntExplodes = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(rule));
        }

        if (world.Storage is { SupportsWorldData: true } && !Save())
            Logger.Scoped("Storage").Warn($"Could not queue game-rule save for world {world.Identifier}.");

        GameRulesChangedPacket packet = new() {
            Data = new GameRulesChangedPacketData {
                Rules = [new Protocol.Types.GameRule {
                    Name = GetName(rule),
                    CanBeModified = true,
                    ValueType = GameRuleValueType.Boolean,
                    BooleanValue = value
                }]
            }
        };
        foreach (Dimension dimension in world.Dimensions.Values) {
            foreach (Player player in dimension.GetPlayers())
                player.SendPacket(packet);
        }

        return true;
    }

    internal GameRulesChangedPacketData CreatePacketData() => new() {
        Rules = [
            CreatePacketRule(GameRule.KeepInventory, keepInventory),
            CreatePacketRule(GameRule.ShowCoordinates, showCoordinates),
            CreatePacketRule(GameRule.FallDamage, fallDamage),
            CreatePacketRule(GameRule.TntExplodes, tntExplodes)
        ]
    };

    internal bool Save() {
        if (world.Storage is not { SupportsWorldData: true } storage)
            return false;

        worldData.Add("keepinventory", new ByteTag { Value = keepInventory ? (byte)1 : (byte)0 });
        worldData.Add("showcoordinates", new ByteTag { Value = showCoordinates ? (byte)1 : (byte)0 });
        worldData.Add("falldamage", new ByteTag { Value = fallDamage ? (byte)1 : (byte)0 });
        worldData.Add("tntexplodes", new ByteTag { Value = tntExplodes ? (byte)1 : (byte)0 });
        worldData.Add("Time", new LongTag { Value = world.GameTime });
        worldData.Add("currentTick", new LongTag { Value = world.GameTime });
        using BinaryStream stream = BinaryStream.Rent(worldData.GetSize());
        BinaryWriter writer = stream.GetWriter();
        BasaltBE.Nbt.Nbt.Write(worldData, ref writer);
        return storage.WriteWorldData(stream.Buffer.Span[..stream.Offset].ToArray());
    }

    private static Protocol.Types.GameRule CreatePacketRule(GameRule rule, bool value) => new() {
        Name = GetName(rule),
        CanBeModified = true,
        ValueType = GameRuleValueType.Boolean,
        BooleanValue = value
    };

    private static string GetName(GameRule rule) => rule switch {
        GameRule.KeepInventory => "keepinventory",
        GameRule.ShowCoordinates => "showcoordinates",
        GameRule.FallDamage => "falldamage",
        GameRule.TntExplodes => "tntexplodes",
        _ => throw new ArgumentOutOfRangeException(nameof(rule))
    };
}

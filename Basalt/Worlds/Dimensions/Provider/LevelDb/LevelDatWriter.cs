using System.Buffers;
using System.Buffers.Binary;
using BinaryWriter = Basalt.Binary.BinaryWriter;

using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Types;
using Basalt.BedrockProtocol.Enums;

namespace Basalt.Core.Worlds.Dimensions.Provider;

public static class LevelDatWriter {
    private const int HeaderVersion = 10;
    private const int StorageVersion = 10;

    public static void Write(string path, World world) {
        CompoundTag root = BuildRootTag(world);
        byte[] nbtPayload = SerializeNbt(root);

        byte[] file = new byte[8 + nbtPayload.Length];
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(0, 4), HeaderVersion);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(4, 4), nbtPayload.Length);
        nbtPayload.CopyTo(file, 8);

        File.WriteAllBytes(path, file);
    }

    private static CompoundTag BuildRootTag(World world) {
        CompoundTag root = new() { Name = string.Empty };

        Dimension? overworld = world.GetDimension(DimensionId.Overworld);
        Vec3 spawn = overworld?.SpawnPosition ?? new Vec3(){ X = 0, Y = 80, Z = 0};
        Difficulty difficulty = overworld?.Difficulty ?? Difficulty.Normal;

        // Core world properties.
        root.Set("StorageVersion", new IntTag { Value = StorageVersion });
        root.Set("WorldVersion", new IntTag { Value = 1 });
        root.Set("LevelName", new StringTag { Value = world.Name });
        root.Set("GameType", new IntTag { Value = 0 });
        root.Set("Difficulty", new IntTag { Value = (int)difficulty });
        root.Set("Generator", new IntTag { Value = 1 });
        root.Set("ForceGameType", new ByteTag { Value = 0 });
        root.Set("IsHardcore", new ByteTag { Value = 0 });
        root.Set("NetworkVersion", new IntTag { Value = Constants.ProtocolVersion });
        root.Set("Platform", new IntTag { Value = 2 });
        root.Set("InventoryVersion", new StringTag { Value = Constants.MinecraftVersion });

        // Spawn position.
        root.Set("SpawnX", new IntTag { Value = (int)spawn.X });
        root.Set("SpawnY", new IntTag { Value = (int)spawn.Y });
        root.Set("SpawnZ", new IntTag { Value = (int)spawn.Z });

        // Time and tick.
        root.Set("Time", new LongTag { Value = world.DayTime });
        root.Set("currentTick", new LongTag { Value = (long)world.TickValue });
        root.Set("LastPlayed", new LongTag { Value = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });
        root.Set("RandomSeed", new LongTag { Value = 0 });

        // Multiplayer.
        root.Set("MultiplayerGame", new ByteTag { Value = 1 });
        root.Set("MultiplayerGameIntent", new ByteTag { Value = 1 });
        root.Set("LANBroadcast", new ByteTag { Value = 1 });
        root.Set("LANBroadcastIntent", new ByteTag { Value = 1 });
        root.Set("XBLBroadcastIntent", new IntTag { Value = 2 });
        root.Set("PlatformBroadcastIntent", new IntTag { Value = 2 });

        // Game rules (vanilla defaults).
        WriteGameRules(root, overworld?.Gamerules);

        // Misc required tags.
        root.Set("commandsEnabled", new ByteTag { Value = 1 });
        root.Set("cheatsEnabled", new ByteTag { Value = 1 });
        root.Set("commandblockoutput", new ByteTag { Value = 1 });
        root.Set("commandblocksenabled", new ByteTag { Value = 1 });
        root.Set("bonusChestEnabled", new ByteTag { Value = 0 });
        root.Set("bonusChestSpawned", new ByteTag { Value = 0 });
        root.Set("startWithMapEnabled", new ByteTag { Value = 0 });
        root.Set("texturePacksRequired", new ByteTag { Value = 0 });
        root.Set("hasBeenLoadedInCreative", new ByteTag { Value = 0 });
        root.Set("hasLockedBehaviorPack", new ByteTag { Value = 0 });
        root.Set("hasLockedResourcePack", new ByteTag { Value = 0 });
        root.Set("immutableWorld", new ByteTag { Value = 0 });
        root.Set("isFromLockedTemplate", new ByteTag { Value = 0 });
        root.Set("isFromWorldTemplate", new ByteTag { Value = 0 });
        root.Set("isWorldTemplateOptionLocked", new ByteTag { Value = 0 });
        root.Set("isSingleUseWorld", new ByteTag { Value = 0 });
        root.Set("isRandomSeedAllowed", new ByteTag { Value = 0 });
        root.Set("isCreatedInEditor", new ByteTag { Value = 0 });
        root.Set("isExportedFromEditor", new ByteTag { Value = 0 });
        root.Set("requiresCopiedPackRemovalCheck", new ByteTag { Value = 0 });
        root.Set("spawnMobs", new ByteTag { Value = 1 });
        root.Set("spawnradius", new IntTag { Value = 10 });
        root.Set("serverChunkTickRange", new IntTag { Value = 4 });
        root.Set("NetherScale", new IntTag { Value = 8 });
        root.Set("daylightCycle", new IntTag { Value = 0 });
        root.Set("editorWorldType", new IntTag { Value = 0 });
        root.Set("eduOffer", new IntTag { Value = 0 });
        root.Set("educationFeaturesEnabled", new ByteTag { Value = 0 });
        root.Set("functioncommandlimit", new IntTag { Value = 10000 });
        root.Set("maxcommandchainlength", new IntTag { Value = 65535 });
        root.Set("limitedWorldDepth", new IntTag { Value = 16 });
        root.Set("limitedWorldWidth", new IntTag { Value = 16 });
        root.Set("permissionsLevel", new IntTag { Value = 0 });
        root.Set("playerPermissionsLevel", new IntTag { Value = 1 });
        root.Set("playerssleepingpercentage", new IntTag { Value = 100 });
        root.Set("randomtickspeed", new IntTag { Value = 1 });
        root.Set("lightningLevel", new FloatTag { Value = 0 });
        root.Set("lightningTime", new IntTag { Value = 0 });
        root.Set("rainLevel", new FloatTag { Value = 0 });
        root.Set("rainTime", new IntTag { Value = 0 });
        root.Set("showcoordinates", new ByteTag { Value = 0 });
        root.Set("showdeathmessages", new ByteTag { Value = 1 });
        root.Set("showtags", new ByteTag { Value = 1 });
        root.Set("pvp", new ByteTag { Value = 1 });
        root.Set("useMsaGamertagsOnly", new ByteTag { Value = 0 });
        root.Set("worldStartCount", new LongTag { Value = 4294967287 });
        root.Set("baseGameVersion", new StringTag { Value = "*" });
        root.Set("BiomeOverride", new StringTag { Value = string.Empty });
        root.Set("FlatWorldLayers", new StringTag { Value = string.Empty });
        root.Set("prid", new StringTag { Value = string.Empty });

        // Version lists.
        root.Set("MinimumCompatibleClientVersion", BuildVersionList(1, 21, 0, 0, 0));
        root.Set("lastOpenedWithVersion", BuildVersionList(1, 21, 0, 0, 0));

        // Abilities compound (vanilla conhtains this).
        CompoundTag abilities = new();
        abilities.Set("attackmobs", new ByteTag { Value = 1 });
        abilities.Set("attackplayers", new ByteTag { Value = 1 });
        abilities.Set("build", new ByteTag { Value = 1 });
        abilities.Set("doorsandswitches", new ByteTag { Value = 1 });
        abilities.Set("flySpeed", new FloatTag { Value = 0.05f });
        abilities.Set("flying", new ByteTag { Value = 0 });
        abilities.Set("instabuild", new ByteTag { Value = 0 });
        abilities.Set("invulnerable", new ByteTag { Value = 0 });
        abilities.Set("lightning", new ByteTag { Value = 0 });
        abilities.Set("mayfly", new ByteTag { Value = 0 });
        abilities.Set("mine", new ByteTag { Value = 1 });
        abilities.Set("op", new ByteTag { Value = 0 });
        abilities.Set("opencontainers", new ByteTag { Value = 1 });
        abilities.Set("teleport", new ByteTag { Value = 0 });
        abilities.Set("walkSpeed", new FloatTag { Value = 0.1f });
        root.Set("abilities", abilities);

        // Experiments compound (empty).
        root.Set("experiments", new CompoundTag());

        // World policies (empty).
        root.Set("world_policies", new CompoundTag());

        return root;
    }

    private static void WriteGameRules(CompoundTag root, DimensionGameRules? rules) {
        root.Set("drowningdamage", new ByteTag { Value = (sbyte)(rules?.DrowningDamage != false ? 1 : 0) });
        root.Set("falldamage", new ByteTag { Value = 1 });
        root.Set("firedamage", new ByteTag { Value = 1 });
        root.Set("freezedamage", new ByteTag { Value = 1 });
        root.Set("dofiretick", new ByteTag { Value = 1 });
        root.Set("domobspawning", new ByteTag { Value = 1 });
        root.Set("domobloot", new ByteTag { Value = 1 });
        root.Set("dotiledrops", new ByteTag { Value = 1 });
        root.Set("doentitydrops", new ByteTag { Value = 1 });
        root.Set("doweathercycle", new ByteTag { Value = 1 });
        root.Set("dodaylightcycle", new ByteTag { Value = (sbyte)(rules?.DaylightCycle != false ? 1 : 0) });
        root.Set("doinsomnia", new ByteTag { Value = 1 });
        root.Set("doimmediaterespawn", new ByteTag { Value = 0 });
        root.Set("dolimitedcrafting", new ByteTag { Value = 0 });
        root.Set("keepinventory", new ByteTag { Value = 0 });
        root.Set("mobgriefing", new ByteTag { Value = 1 });
        root.Set("naturalregeneration", new ByteTag { Value = 1 });
        root.Set("sendcommandfeedback", new ByteTag { Value = 1 });
        root.Set("tntexplodes", new ByteTag { Value = 1 });
        root.Set("tntexplosiondropdecay", new ByteTag { Value = 0 });
        root.Set("respawnblocksexplode", new ByteTag { Value = 1 });
        root.Set("showbordereffect", new ByteTag { Value = 1 });
        root.Set("showrecipemessages", new ByteTag { Value = 1 });
        root.Set("recipesunlock", new ByteTag { Value = 1 });
        root.Set("projectilescanbreakblocks", new ByteTag { Value = 1 });
        root.Set("showdaysplayed", new ByteTag { Value = 0 });
    }

    private static ListTag BuildVersionList(int major, int minor, int patch, int revision, int beta) {
        ListTag list = new();
        list.Values.Add(new IntTag { Value = major });
        list.Values.Add(new IntTag { Value = minor });
        list.Values.Add(new IntTag { Value = patch });
        list.Values.Add(new IntTag { Value = revision });
        list.Values.Add(new IntTag { Value = beta });
        return list;
    }

    private static byte[] SerializeNbt(CompoundTag tag) {
        int size = 4096;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        while (true) {
            int offset = 0;
            BinaryWriter writer = new(buffer, ref offset);

            try {
                NBT.WriteTag(writer, tag, new TagOptions(Name: true, Type: true, VarInt: false));
                byte[] data = writer.GetProcessedBytes().ToArray();
                ArrayPool<byte>.Shared.Return(buffer);
                return data;
            }
            catch (Exception exception) when (
                exception is ArgumentOutOfRangeException or IndexOutOfRangeException) {
                ArrayPool<byte>.Shared.Return(buffer);
                size <<= 1;
                if (size > 16 * 1024 * 1024) {
                    throw;
                }

                buffer = ArrayPool<byte>.Shared.Rent(size);
            }
            catch {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }
        }
    }
}

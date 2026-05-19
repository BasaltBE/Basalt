using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.Traits;
using Basalt.Item;
using Basalt.Item.Traits;

using System.Diagnostics;

namespace Basalt.Entity.Traits.PlayerTraits;

public sealed class DebugTrait : PlayerTrait
{
    private const double TargetTps = 20.0;
    private const ulong SendIntervalTicks = 20;

    public new static string Identifier => "debug";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];

    private ulong _lastSentTick;
    private long _lastSentTimestamp;
    // its so goody cause it flickers between 19.7 and 20.3
    private double _smoothedTps;
    private double _averageMspt;
    private bool _gaveDebugItems;

    public DebugTrait(Entity entity) : base(entity)
    {
    }

    public override void OnSpawn(Basalt.Entity.Traits.Types.EntitySpawnOptions details)
    {
        _lastSentTick = Player.Dimension?.World?.CurrentTick ?? 0;
        _lastSentTimestamp = Stopwatch.GetTimestamp();
        _smoothedTps = 0;
        _averageMspt = 0;
        if (!_gaveDebugItems)
        {
            EntityInventoryTrait? inventory = Player.GetTrait<EntityInventoryTrait>();
            if (inventory is not null)
            {
                string[] debugItems =
                [
                    ItemIdentifier.Barrel.ToIdentifier(),
                    ItemIdentifier.Chest.ToIdentifier(),
                    ItemIdentifier.Stick.ToIdentifier(),
                    ItemIdentifier.DiamondPickaxe.ToIdentifier(),
                    ItemIdentifier.GrassBlock.ToIdentifier(),
                    ItemIdentifier.Stone.ToIdentifier(),
                    ItemIdentifier.Dirt.ToIdentifier(),
                ];

                int[] targetSlots = [0, 9, 10, 11, 12];
                for (int i = 0; i < debugItems.Length; i++)
                {
                    string identifier = debugItems[i];
                    ItemType? type = ItemType.Get(identifier);
                    if (type is null)
                    {
                        continue;
                    }

                    ushort amount = identifier is "minecraft:diamond_pickaxe" or "minecraft:stick" ? (ushort)1 : (ushort)64;
                    ItemStack itemStack = new(type, amount);
                    if (identifier == "minecraft:stick")
                    {
                        itemStack.AddTrait(new ItemDebugTrait(itemStack));
                    }

                    inventory.Container.AddItem(itemStack);
                }
            }

            _gaveDebugItems = true;
        }
    }

    public override void OnTick(TraitOnTickDetails details)
    {
        if (!Player.IsAlive || details.CurrentTick - _lastSentTick < SendIntervalTicks)
        {
            return;
        }

        try
        {
            long nowTimestamp = Stopwatch.GetTimestamp();
            ulong tickDelta = details.CurrentTick - _lastSentTick;
            long timestampDelta = nowTimestamp - _lastSentTimestamp;
            if (tickDelta == 0 || timestampDelta <= 0)
            {
                return;
            }

            double elapsedMs = timestampDelta * 1000.0 / Stopwatch.Frequency;
            double rawTps = tickDelta * 1000.0 / elapsedMs;
            _smoothedTps = _smoothedTps == 0 ? rawTps : _smoothedTps + ((rawTps - _smoothedTps) * 0.2);
            double tps = _smoothedTps;
            double mspt = Player.Dimension?.World?.LastTickWorkMs ?? 0;
            _averageMspt = _averageMspt == 0 ? mspt : _averageMspt + ((mspt - _averageMspt) * 0.2);
            double workingSetMb = Environment.WorkingSet / (1024.0 * 1024.0);
            int chunksLoaded = Player.Dimension?.ChunkCount ?? 0;

            TextPacket packet = new()
            {
                NeedsTranslation = false,
                VariantType = TextVariantType.MessageOnly,
                Variant = new TextVariant
                {
                    Type = TextType.Tip,
                    Message = $"§aTPS: §f{tps:0.0}§8/§f{TargetTps:0.0} §8| §aMSPT: §f{mspt:0.00} §8| §aA/MSPT: §f{_averageMspt:0.00} §8| §aRAM: §f{workingSetMb:0.0}MB §8| §aChunks: §f{chunksLoaded}"
                },
                Xuid = string.Empty,
                PlatformChatId = string.Empty,
                FilteredMessage = null
            };

            Player.Send(packet);
            _lastSentTick = details.CurrentTick;
            _lastSentTimestamp = nowTimestamp;
        }
        catch (Exception exception)
        {
            Logger.Warn($"[{Player.Username}] DebugTrait exception: {exception}");
        }
    }

    public override EntityTrait Clone(Entity entity)
    {
        return new DebugTrait(entity);
    }
}

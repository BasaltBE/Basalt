namespace Basalt.Core.Player.Traits;

using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.Core.Item;
using Basalt.Core.Item.Traits;
using Basalt.Core.Worlds;

using Entity = Basalt.Core.Entities.Entity;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Traits;

// using Player = Player.Player;


public sealed class DebugTrait : PlayerTrait {
    private const double TargetTps = 20.0;
    private const ulong SendIntervalTicks = 20;

    public new static string Identifier => "debug";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];

    private ulong _lastSentTick;
    private double _averageMspt;
    // private bool _gaveDebugItems;

    public DebugTrait(Entity entity) : base(entity) {
    }

    public override void OnSpawn(EntitySpawnOptions details) {
        _lastSentTick = Player.Dimension?.World is Tickable tickable ? tickable.TickValue : 0;
        _averageMspt = 0;
        // if (!_gaveDebugItems)
        // {
        //     EntityInventoryTrait? inventory = Player.GetTrait<EntityInventoryTrait>();
        //     if (inventory is not null)
        //     {
        //         string[] debugItems =
        //         [
        //             ItemIdentifier.Barrel.ToIdentifier(),
        //             ItemIdentifier.Chest.ToIdentifier(),
        //             ItemIdentifier.Stick.ToIdentifier(),
        //             ItemIdentifier.DiamondPickaxe.ToIdentifier(),
        //             ItemIdentifier.GrassBlock.ToIdentifier(),
        //             ItemIdentifier.Stone.ToIdentifier(),
        //             ItemIdentifier.Dirt.ToIdentifier(),
        //         ];

        //         int[] targetSlots = [0, 9, 10, 11, 12];
        //         for (int i = 0; i < debugItems.Length; i++)
        //         {
        //             string identifier = debugItems[i];
        //             ItemType? type = ItemType.Get(identifier);
        //             if (type is null)
        //             {
        //                 continue;
        //             }

        //             ushort amount = identifier is "minecraft:diamond_pickaxe" or "minecraft:stick" ? (ushort)1 : (ushort)64;
        //             ItemStack itemStack = new(type, amount);
        //             if (identifier == "minecraft:stick")
        //             {
        //                 itemStack.AddTrait(new ItemDebugTrait(itemStack));
        //             }

        //             inventory.Container.AddItem(itemStack);
        //         }
        //     }

        //     _gaveDebugItems = true;
        // }
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (!Player.IsAlive || details.CurrentTick - _lastSentTick < SendIntervalTicks) {
            return;
        }

        try {
            double tps = Player.Dimension?.World?.Server?.Tps ?? TargetTps;
            double mspt = Player.Dimension?.World?.Server?.TickWork ?? 0;
            _averageMspt = _averageMspt == 0 ? mspt : _averageMspt + ((mspt - _averageMspt) * 0.2);
            double workingSetMb = Environment.WorkingSet / (1024.0 * 1024.0);
            int chunksLoaded = Player.Dimension?.ChunkCount ?? 0;

            TextPacket packet = new() {
                NeedsTranslation = false,
                VariantType = TextVariantType.MessageOnly,
                Variant = new TextVariant {
                    Type = TextType.Tip,
                    Message = $"§aTPS: §f{tps:0.0}§8/§f{TargetTps:0.0} §8| §aMSPT: §f{mspt:0.00} §8| §aA/MSPT: §f{_averageMspt:0.00} §8| §aRAM: §f{workingSetMb:0.0}MB §8| §aChunks: §f{chunksLoaded}"
                },
                Xuid = string.Empty,
                PlatformChatId = string.Empty,
                FilteredMessage = null
            };

            Player.Send(packet);
            _lastSentTick = details.CurrentTick;
        }
        catch (Exception exception) {
            Logger.Warn($"[{Player.Username}] DebugTrait exception: {exception}");
        }
    }

    public override EntityTrait Clone(Entity entity) {
        return new DebugTrait(entity);
    }
}







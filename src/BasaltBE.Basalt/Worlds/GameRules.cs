using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using GameRule = BasaltBE.Basalt.Worlds.Types.GameRule;

namespace BasaltBE.Basalt.Worlds;

public sealed class GameRules {
    private readonly World world;
    private bool keepInventory;
    private bool showCoordinates;
    private bool fallDamage = true;

    internal GameRules(World world) => this.world = world;

    public bool Get(GameRule rule) => rule switch {
        GameRule.KeepInventory => keepInventory,
        GameRule.ShowCoordinates => showCoordinates,
        GameRule.FallDamage => fallDamage,
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
            default:
                throw new ArgumentOutOfRangeException(nameof(rule));
        }

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
                player.Network.SendPacket(player, packet);
        }

        return true;
    }

    internal GameRulesChangedPacketData CreatePacketData() => new() {
        Rules = [
            CreatePacketRule(GameRule.KeepInventory, keepInventory),
            CreatePacketRule(GameRule.ShowCoordinates, showCoordinates),
            CreatePacketRule(GameRule.FallDamage, fallDamage)
        ]
    };

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
        _ => throw new ArgumentOutOfRangeException(nameof(rule))
    };
}

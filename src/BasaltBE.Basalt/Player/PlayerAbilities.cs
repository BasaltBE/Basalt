using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities;

public sealed class PlayerAbilities {
    const uint BaseAbilities = (1U << (int)PlayerAbility.Build)
        | (1U << (int)PlayerAbility.Mine)
        | (1U << (int)PlayerAbility.DoorsAndSwitches)
        | (1U << (int)PlayerAbility.OpenContainers)
        | (1U << (int)PlayerAbility.AttackPlayers)
        | (1U << (int)PlayerAbility.AttackMobs);
    const uint SpeedAbilities = (1U << (int)PlayerAbility.FlySpeed)
        | (1U << (int)PlayerAbility.WalkSpeed)
        | (1U << (int)PlayerAbility.VerticalFlySpeed);
    const uint SurvivalAbilities = (1U << (int)PlayerAbility.MayFly)
        | (1U << (int)PlayerAbility.InstantBuild)
        | (1U << (int)PlayerAbility.Invulnerable)
        | (1U << (int)PlayerAbility.Flying)
        | (1U << (int)PlayerAbility.NoClip);
    uint enabled = BaseAbilities;
    uint controlled = BaseAbilities | SpeedAbilities | SurvivalAbilities;

    public float FlySpeed = 0.05f;
    public float VerticalFlySpeed = 1.0f;
    public float WalkSpeed = 0.1f;
    public bool Dirty;

    public bool Get(PlayerAbility ability) => (enabled & (1U << (int)ability)) != 0;

    public bool Set(PlayerAbility ability, bool value) {
        uint mask = 1U << (int)ability;
        bool changed = (controlled & mask) == 0 || Get(ability) != value;
        controlled |= mask;
        if (value)
            enabled |= mask;
        else
            enabled &= ~mask;
        Dirty |= changed;
        return changed;
    }

    public bool SetSpeeds(float fly, float verticalFly, float walk) {
        if (FlySpeed == fly && VerticalFlySpeed == verticalFly && WalkSpeed == walk)
            return false;

        FlySpeed = fly;
        VerticalFlySpeed = verticalFly;
        WalkSpeed = walk;
        Dirty = true;
        return true;
    }

    internal UpdateAbilitiesPacket CreateUpdate(long playerRawId) {
        Dirty = false;
        return new UpdateAbilitiesPacket {
            Data = new SerializedAbilitiesData {
                TargetPlayerRawId = playerRawId,
                PlayerPermissions = PlayerPermissionLevel.Member,
                CommandPermissions = CommandPermissionLevel.Any,
                Layers = [new SerializedAbilitiesLayer {
                    Layer = 1,
                    AbilitiesSet = controlled,
                    AbilityValues = enabled,
                    FlySpeed = FlySpeed,
                    VerticalFlySpeed = VerticalFlySpeed,
                    WalkSpeed = WalkSpeed
                }]
            }
        };
    }
}

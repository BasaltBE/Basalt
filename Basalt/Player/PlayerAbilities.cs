using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

namespace Basalt.Core.Player;

public enum PlayerAbility : byte {
    Build = 0,
    Mine = 1,
    DoorsAndSwitches = 2,
    OpenContainers = 3,
    AttackPlayers = 4,
    AttackMobs = 5,
    OperatorCommands = 6,
    Teleport = 7,
    Invulnerable = 8,
    Flying = 9,
    MayFly = 10,
    InstantBuild = 11,
    Lightning = 12,
    FlySpeed = 13,
    WalkSpeed = 14,
    Muted = 15,
    WorldBuilder = 16,
    NoClip = 17,
    PrivilegedBuilder = 18,
    VerticalFlySpeed = 19
}

public sealed class PlayerAbilities {
    private static readonly PlayerAbility[] BaseAbilities = [
        PlayerAbility.Build,
        PlayerAbility.Mine,
        PlayerAbility.DoorsAndSwitches,
        PlayerAbility.OpenContainers,
        PlayerAbility.AttackPlayers,
        PlayerAbility.AttackMobs
    ];

    private readonly HashSet<PlayerAbility> _enabled = [];
    private readonly HashSet<PlayerAbility> _controlled = [];

    public float FlySpeed { get; set; } = 0.05f;
    public float VerticalFlySpeed { get; set; } = 1.0f;
    public float WalkSpeed { get; set; } = 0.1f;

    public bool GetAbility(PlayerAbility ability) {
        return _enabled.Contains(ability);
    }

    public void SetAbility(PlayerAbility ability, bool enabled) {
        _controlled.Add(ability);

        if (enabled) {
            _enabled.Add(ability);
            return;
        }

        _enabled.Remove(ability);
    }

    public void SetGamemode(GameType gamemode) {
        _enabled.Clear();
        _controlled.Clear();

        Enable(BaseAbilities);

        SetAbility(PlayerAbility.MayFly, gamemode is GameType.Creative or GameType.Spectator);
        SetAbility(PlayerAbility.InstantBuild, gamemode is GameType.Creative or GameType.Spectator);
        SetAbility(PlayerAbility.Invulnerable, gamemode == GameType.Spectator);
        SetAbility(PlayerAbility.Flying, gamemode == GameType.Spectator);
        SetAbility(PlayerAbility.NoClip, gamemode == GameType.Spectator);
    }

    public void SetOperator(bool isOperator) {
        if (isOperator) {
            SetAbility(PlayerAbility.OperatorCommands, true);
            SetAbility(PlayerAbility.Teleport, true);
            return;
        }

        Disable(PlayerAbility.OperatorCommands);
        Disable(PlayerAbility.Teleport);
    }

    public UpdateAbilitiesPacket CreatePacket(
        long entityUniqueId,
        bool isOperator
    ) {
        return new UpdateAbilitiesPacket {
            Data = new() {
                CommandPermissions = isOperator ? (byte)2 : (byte)0,

                Layers = [
                    ToLayer()
                ],

                PlayerPermissions = (sbyte)(isOperator ? PlayerPermissionLevel.Operator : PlayerPermissionLevel.Member),

                TargetPlayerRawId = entityUniqueId
            }
        };
    }

    public SerializedAbilitiesLayer ToLayer() {
        return new SerializedAbilitiesLayer {
            Layer = 1,
            AbilitiesSet = CreateMask(_controlled)
                | (1U << (int)PlayerAbility.FlySpeed)
                | (1U << (int)PlayerAbility.WalkSpeed)
                | (1U << (int)PlayerAbility.VerticalFlySpeed),
            AbilityValues = CreateMask(_enabled),
            FlySpeed = FlySpeed,
            VerticalFlySpeed = VerticalFlySpeed,
            WalkSpeed = WalkSpeed
        };
    }

    private void Enable(params PlayerAbility[] abilities) {
        for (int i = 0; i < abilities.Length; i++) {
            PlayerAbility ability = abilities[i];

            _controlled.Add(ability);
            _enabled.Add(ability);
        }
    }

    private void Disable(params PlayerAbility[] abilities) {
        for (int i = 0; i < abilities.Length; i++) {
            PlayerAbility ability = abilities[i];
            _controlled.Remove(ability);
            _enabled.Remove(ability);
        }
    }

    private static uint CreateMask(
        IEnumerable<PlayerAbility> abilities
    ) {
        uint mask = 0;

        foreach (PlayerAbility ability in abilities) {
            mask |= 1U << (int)ability;
        }

        return mask;
    }
}

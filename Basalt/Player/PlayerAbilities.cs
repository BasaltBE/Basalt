using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Core.Player;

public sealed class PlayerAbilities
{
    private readonly HashSet<AbilityIndex> _enabled = [];
    private static readonly AbilityIndex[] BaseAbilities =
    [
        AbilityIndex.Build,
        AbilityIndex.Mine,
        AbilityIndex.DoorsAndSwitches,
        AbilityIndex.OpenContainers,
        AbilityIndex.AttackPlayers,
        AbilityIndex.AttackMobs,
        AbilityIndex.FlySpeed,
        AbilityIndex.WalkSpeed,
        AbilityIndex.VerticalFlySpeed
    ];

    public bool GetAbility(AbilityIndex ability)
    {
        return _enabled.Contains(ability);
    }

    public void SetAbility(AbilityIndex ability, bool enabled)
    {
        if (enabled)
        {
            _enabled.Add(ability);
            return;
        }

        _enabled.Remove(ability);
    }

    public void SetGamemode(Gamemode gamemode)
    {
        _enabled.Clear();
        Enable(BaseAbilities);

        if (gamemode is Gamemode.Creative or Gamemode.Spectator)
        {
            Enable(AbilityIndex.MayFly, AbilityIndex.InstantBuild);
        }

        if (gamemode == Gamemode.Spectator)
        {
            Enable(AbilityIndex.Invulnerable, AbilityIndex.Flying, AbilityIndex.NoClip);
        }
    }

    public void SetOperator(bool isOperator)
    {
        SetAbility(AbilityIndex.OperatorCommands, isOperator);
        SetAbility(AbilityIndex.Teleport, isOperator);
    }

    public UpdateAbilitiesPacket CreatePacket(long entityUniqueId, bool isOperator)
    {
        return new UpdateAbilitiesPacket
        {
            EntityUniqueId = entityUniqueId,
            PlayerPermission = isOperator ? PlayerPermissionLevel.Operator : PlayerPermissionLevel.Member,
            CommandPermission = isOperator ? CommandPermissionLevel.Admin : CommandPermissionLevel.Any,
            Layers = [ToLayer()]
        };
    }

    public AbilityLayer ToLayer()
    {
        uint values = 0;
        foreach (AbilityIndex ability in _enabled)
        {
            values |= 1U << (int)ability;
        }

        return new AbilityLayer
        {
            Type = AbilityLayerType.Base,
            Abilities = (1U << (int)AbilityIndex.Count) - 1U,
            Values = values
        };
    }

    private void Enable(params AbilityIndex[] abilities)
    {
        for (int i = 0; i < abilities.Length; i++)
        {
            _enabled.Add(abilities[i]);
        }
    }
}







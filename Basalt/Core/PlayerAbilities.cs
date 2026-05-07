using Basalt.Protocol.Enums;

namespace Basalt.Core;

public sealed class PlayerAbilities
{
    private readonly HashSet<AbilityIndex> _enabled = [];

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
}

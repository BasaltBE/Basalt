using Basalt.Protocol.Enums;

namespace Basalt.Entity;

public sealed class EntityActorFlags
{
    private readonly Entity _entity;
    private readonly HashSet<ActorFlag> _flags = [];

    public EntityActorFlags(Entity entity)
    {
        _entity = entity;
    }

    public bool GetActorFlag(ActorFlag flag)
    {
        return _flags.Contains(flag);
    }

    public void SetActorFlag(ActorFlag flag, bool value)
    {
        bool changed;
        if (value)
        {
            changed = _flags.Add(flag);
        }
        else
        {
            changed = _flags.Remove(flag);
        }

        if (changed)
        {
            _entity.SendActorFlagsUpdate();
        }
    }

    public long ToMask()
    {
        long mask = 0;
        foreach (ActorFlag flag in _flags)
        {
            int bit = (int)flag;
            if ((uint)bit >= 63)
            {
                continue;
            }

            mask |= 1L << bit;
        }

        return mask;
    }
}

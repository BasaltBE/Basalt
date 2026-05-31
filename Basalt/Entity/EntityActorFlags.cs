namespace Basalt.Server.Entity;

using Basalt.Protocol.Enums;

public sealed class EntityActorFlags
{
    private readonly Entity _entity;
    private UInt128 _value;

    public EntityActorFlags(Entity entity)
    {
        _entity = entity;
    }

    public bool GetActorFlag(ActorFlag flag)
    {
        int shift = (int)flag;
        return ((_value >> shift) & 1) != 0;
    }

    public void SetActorFlag(ActorFlag flag, bool value)
    {
        int shift = (int)flag;
        UInt128 mask = (UInt128)1 << shift;
        UInt128 before = _value;
        if (value)
        {
            _value |= mask;
        }
        else
        {
            _value &= ~mask;
        }

        if (_value != before)
        {
            _entity.SendActorFlagsUpdate();
        }
    }

    public long Lower64()
    {
        return unchecked((long)(ulong)(_value & ulong.MaxValue));
    }

    public long Upper64()
    {
        return unchecked((long)(ulong)(_value >> 64));
    }
}







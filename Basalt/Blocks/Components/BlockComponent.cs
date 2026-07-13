namespace Basalt.Core.Blocks.Components;

using Basalt.Protocol.Nbt;

public abstract class BlockComponent
{
    public static string Identifier => string.Empty;

    public abstract string ComponentIdentifier { get; }

    public virtual void OnRead(CompoundTag tag)
    {
    }

    public virtual void OnWrite(CompoundTag tag)
    {
    }

    public virtual BlockComponent Clone()
    {
        return (BlockComponent)MemberwiseClone();
    }
}

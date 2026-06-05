using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class AbilityLayer : DataType
{
    /// <summary>
    /// Ability layer this set of abilities belongs to.
    /// </summary>
    public AbilityLayerType Type;

    /// <summary>
    /// Abilities that are available in this layer.
    /// </summary>
    public uint Abilities;

    /// <summary>
    /// Enabled values for the available abilities.
    /// </summary>
    public uint Values;

    /// <summary>
    /// Horizontal flying speed.
    /// </summary>
    public float FlySpeed = 0.05f;

    /// <summary>
    /// Vertical flying speed.
    /// </summary>
    public float VerticalFlySpeed = 1.0f;

    /// <summary>
    /// Walking speed.
    /// </summary>
    public float WalkSpeed = 0.1f;

    public void Read(BinaryReader reader)
    {
        Type = (AbilityLayerType)reader.ReadUInt16(true);
        Abilities = reader.ReadUInt32(true);
        Values = reader.ReadUInt32(true);
        FlySpeed = reader.ReadF32(true);
        VerticalFlySpeed = reader.ReadF32(true);
        WalkSpeed = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteUInt16((ushort)Type, true);
        writer.WriteUInt32(Abilities, true);
        writer.WriteUInt32(Values, true);
        writer.WriteF32(FlySpeed, true);
        writer.WriteF32(VerticalFlySpeed, true);
        writer.WriteF32(WalkSpeed, true);
    }
}

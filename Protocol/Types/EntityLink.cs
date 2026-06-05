using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class EntityLink : DataType
{
    /// <summary>
    /// Unique id of the ridden actor.
    /// </summary>
    public long RiddenEntityUniqueId;

    /// <summary>
    /// Unique id of the rider actor.
    /// </summary>
    public long RiderEntityUniqueId;

    /// <summary>
    /// Link type value.
    /// </summary>
    public byte Type;

    /// <summary>
    /// Whether this link is immediate.
    /// </summary>
    public bool Immediate;

    /// <summary>
    /// Whether the rider initiated this link.
    /// </summary>
    public bool RiderInitiated;

    /// <summary>
    /// Vehicle angular velocity value.
    /// </summary>
    public float VehicleAngularVelocity;

    public void Read(BinaryReader reader)
    {
        RiddenEntityUniqueId = reader.ReadVarLong();
        RiderEntityUniqueId = reader.ReadVarLong();
        Type = reader.ReadUInt8();
        Immediate = reader.ReadBool();
        RiderInitiated = reader.ReadBool();
        VehicleAngularVelocity = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarLong(RiddenEntityUniqueId);
        writer.WriteVarLong(RiderEntityUniqueId);
        writer.WriteUInt8(Type);
        writer.WriteBool(Immediate);
        writer.WriteBool(RiderInitiated);
        writer.WriteF32(VehicleAngularVelocity, true);
    }
}

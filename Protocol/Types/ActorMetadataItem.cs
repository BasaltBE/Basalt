using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ActorMetadataItem : DataType
{
    public ActorDataId Id { get; set; }
    public ActorDataType Type { get; set; }
    public object Value { get; set; } = 0;

    public void Read(ref BinaryReader reader)
    {
        Id = (ActorDataId)reader.ReadVarInt();
        Type = (ActorDataType)reader.ReadVarInt();
        Value = Type switch
        {
            ActorDataType.Byte => reader.ReadInt8(),
            ActorDataType.Short => reader.ReadInt16(true),
            ActorDataType.Int => reader.ReadZigZag(),
            ActorDataType.Float => reader.ReadF32(true),
            ActorDataType.String => reader.ReadVarString(),
            ActorDataType.Long => reader.ReadZigZong(),
            ActorDataType.Vec3 =>
                new Vec3f
                {
                    X = reader.ReadF32(true),
                    Y = reader.ReadF32(true),
                    Z = reader.ReadF32(true)
                },
            _ => throw new NotSupportedException($"Unsupported ActorDataType: {Type}")
        };
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarInt((int)Id);
        writer.WriteVarInt((int)Type);
        switch (Type)
        {
            case ActorDataType.Byte:
                writer.WriteInt8(Convert.ToSByte(Value));
                break;
            case ActorDataType.Short:
                writer.WriteInt16(Convert.ToInt16(Value), true);
                break;
            case ActorDataType.Int:
                writer.WriteZigZag(Convert.ToInt32(Value));
                break;
            case ActorDataType.Float:
                writer.WriteF32(Convert.ToSingle(Value), true);
                break;
            case ActorDataType.String:
                writer.WriteVarString(Convert.ToString(Value) ?? string.Empty);
                break;
            case ActorDataType.Long:
                writer.WriteZigZong(Convert.ToInt64(Value));
                break;
            case ActorDataType.Vec3:
                if (Value is not Vec3f vec3)
                {
                    throw new InvalidOperationException("Actor metadata Vec3 value must be Vec3f.");
                }

                writer.WriteF32(vec3.X, true);
                writer.WriteF32(vec3.Y, true);
                writer.WriteF32(vec3.Z, true);
                break;
            default:
                throw new NotSupportedException($"Unsupported ActorDataType: {Type}");
        }
    }
}

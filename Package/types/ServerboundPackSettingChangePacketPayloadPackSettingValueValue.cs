#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public enum ServerboundPackSettingChangePacketPayloadPackSettingValueValueKind : uint {
    FloatValue = 0,
    BooleanValue = 1,
    StringValue = 2,
}

public sealed class ServerboundPackSettingChangePacketPayloadPackSettingValueValue {
    public ServerboundPackSettingChangePacketPayloadPackSettingValueValueKind Kind;
    public float FloatValue;
    public bool BooleanValue;
    public string StringValue = string.Empty;

    public void Read(BinaryReader reader) {
        Kind = (ServerboundPackSettingChangePacketPayloadPackSettingValueValueKind)reader.ReadVarUInt();
        switch (Kind) {
            case ServerboundPackSettingChangePacketPayloadPackSettingValueValueKind.FloatValue:
                FloatValue = reader.ReadF32(true);
                break;
            case ServerboundPackSettingChangePacketPayloadPackSettingValueValueKind.BooleanValue:
                BooleanValue = reader.ReadBool();
                break;
            case ServerboundPackSettingChangePacketPayloadPackSettingValueValueKind.StringValue:
                StringValue = reader.ReadVarString();
                break;
            default:
                throw new FormatException($"Unknown ServerboundPackSettingChangePacketPayloadPackSettingValueValue variant {Kind}.");
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(((uint)Kind));
        switch (Kind) {
            case ServerboundPackSettingChangePacketPayloadPackSettingValueValueKind.FloatValue:
                writer.WriteF32(FloatValue, true);
                break;
            case ServerboundPackSettingChangePacketPayloadPackSettingValueValueKind.BooleanValue:
                writer.WriteBool(BooleanValue);
                break;
            case ServerboundPackSettingChangePacketPayloadPackSettingValueValueKind.StringValue:
                writer.WriteVarString(StringValue);
                break;
            default:
                throw new InvalidOperationException($"Unsupported ServerboundPackSettingChangePacketPayloadPackSettingValueValue variant {Kind}.");
        }
    }
}

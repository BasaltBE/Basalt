using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CameraAimAssistCategoryReference {
    public string? StringValue { get; private set; }
    public string? String2Value { get; private set; }

    public bool IsString => StringValue is not null;
    public bool IsString2 => String2Value is not null;

    public CameraAimAssistCategoryReference() {
    }

    public static CameraAimAssistCategoryReference FromString(string value) {
        ArgumentNullException.ThrowIfNull(value);
        return new CameraAimAssistCategoryReference { StringValue = value };
    }

    public static CameraAimAssistCategoryReference FromString2(string value) {
        ArgumentNullException.ThrowIfNull(value);
        return new CameraAimAssistCategoryReference { String2Value = value };
    }

    public void Read(BinaryReader reader) {
        throw new NotSupportedException("CameraAimAssistCategoryReference has no standalone wire discriminator in the protocol schema.");
    }

    public void Write(BinaryWriter writer) {
        throw new NotSupportedException("CameraAimAssistCategoryReference has no standalone wire discriminator in the protocol schema.");
    }
}

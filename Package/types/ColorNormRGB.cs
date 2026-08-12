#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ColorNormRGB {
    public string? HexValue { get; private set; }
    public IReadOnlyList<float>? ComponentsValue { get; private set; }

    public bool IsHex => HexValue is not null;
    public bool IsComponents => ComponentsValue is not null;

    public ColorNormRGB() {
    }

    public ColorNormRGB(string value) {
        ArgumentNullException.ThrowIfNull(value);

        HexValue = value;
    }

    public ColorNormRGB(IReadOnlyList<float> value) {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Count != 3) {
            throw new ArgumentException(
                "ColorNormRGB must contain exactly 3 items.",
                nameof(value)
            );
        }

        ComponentsValue = value;
    }

    public static ColorNormRGB FromHex(string value) {
        ArgumentNullException.ThrowIfNull(value);
        return new ColorNormRGB { HexValue = value };
    }

    public static ColorNormRGB FromComponents(IReadOnlyList<float> value) {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Count != 3) {
            throw new ArgumentException(
                "ColorNormRGB must contain exactly 3 items.",
                nameof(value)
            );
        }
        return new ColorNormRGB { ComponentsValue = value };
    }

    public void Read(BinaryReader reader) {
        throw new NotSupportedException("ColorNormRGB has no standalone wire discriminator in the protocol schema.");
    }

    public void Write(BinaryWriter writer) {
        throw new NotSupportedException("ColorNormRGB has no standalone wire discriminator in the protocol schema.");
    }
}

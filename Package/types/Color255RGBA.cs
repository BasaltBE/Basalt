using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class Color255RGBA {
    public string? HexValue { get; private set; }
    public IReadOnlyList<int>? ComponentsValue { get; private set; }

    public bool IsHex => HexValue is not null;
    public bool IsComponents => ComponentsValue is not null;

    public Color255RGBA() {
    }

    public Color255RGBA(string value) {
        ArgumentNullException.ThrowIfNull(value);

        HexValue = value;
    }

    public Color255RGBA(IReadOnlyList<int> value) {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Count != 4) {
            throw new ArgumentException(
                "Color255RGBA must contain exactly 4 items.",
                nameof(value)
            );
        }

        ComponentsValue = value;
    }

    public static Color255RGBA FromHex(string value) {
        ArgumentNullException.ThrowIfNull(value);
        return new Color255RGBA { HexValue = value };
    }

    public static Color255RGBA FromComponents(IReadOnlyList<int> value) {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Count != 4) {
            throw new ArgumentException(
                "Color255RGBA must contain exactly 4 items.",
                nameof(value)
            );
        }
        return new Color255RGBA { ComponentsValue = value };
    }

    public void Read(BinaryReader reader) {
        throw new NotSupportedException("Color255RGBA has no standalone wire discriminator in the protocol schema.");
    }

    public void Write(BinaryWriter writer) {
        throw new NotSupportedException("Color255RGBA has no standalone wire discriminator in the protocol schema.");
    }
}

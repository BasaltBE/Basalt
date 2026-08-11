using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class Color255RGB {
    public string? HexValue { get; private set; }
    public IReadOnlyList<int>? ComponentsValue { get; private set; }

    public bool IsHex => HexValue is not null;
    public bool IsComponents => ComponentsValue is not null;

    public Color255RGB() {
    }

    public Color255RGB(string value) {
        ArgumentNullException.ThrowIfNull(value);

        HexValue = value;
    }

    public Color255RGB(IReadOnlyList<int> value) {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Count != 3) {
            throw new ArgumentException(
                "Color255RGB must contain exactly 3 items.",
                nameof(value)
            );
        }

        ComponentsValue = value;
    }

    public static Color255RGB FromHex(string value) {
        ArgumentNullException.ThrowIfNull(value);
        return new Color255RGB { HexValue = value };
    }

    public static Color255RGB FromComponents(IReadOnlyList<int> value) {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Count != 3) {
            throw new ArgumentException(
                "Color255RGB must contain exactly 3 items.",
                nameof(value)
            );
        }
        return new Color255RGB { ComponentsValue = value };
    }

    public void Read(BinaryReader reader) {
        throw new NotSupportedException("Color255RGB has no standalone wire discriminator in the protocol schema.");
    }

    public void Write(BinaryWriter writer) {
        throw new NotSupportedException("Color255RGB has no standalone wire discriminator in the protocol schema.");
    }
}

#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class Empty : LegacyTelemetryEventEventDataVariant {
    #pragma warning disable CA1822

    public void Read(BinaryReader reader) {
    }

    public void Write(BinaryWriter writer) {
    }

    #pragma warning restore CA1822
}

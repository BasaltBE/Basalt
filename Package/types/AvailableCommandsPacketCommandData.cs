#nullable enable

using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AvailableCommandsPacketCommandData {
    public string Name = string.Empty;
    public string Description = string.Empty;
    public ushort Flags;
    public CommandPermissionLevel PermissionLevel;
    public int AliasEnum;
    public List<uint> CommandDataChainedSubcommandIndexes = [];
    public List<AvailableCommandsPacketOverloadData> Overloads = [];

    public void Read(BinaryReader reader) {
        Name = reader.ReadVarString();
        Description = reader.ReadVarString();
        Flags = reader.ReadUInt16(true);
        string enumText6 = reader.ReadVarString();
        PermissionLevel = enumText6 switch {
            "any" => global::BedrockProtocol.Enums.CommandPermissionLevel.Any,
            "gamedirectors" => global::BedrockProtocol.Enums.CommandPermissionLevel.GameDirectors,
            "admin" => global::BedrockProtocol.Enums.CommandPermissionLevel.Admin,
            "host" => global::BedrockProtocol.Enums.CommandPermissionLevel.Host,
            "owner" => global::BedrockProtocol.Enums.CommandPermissionLevel.Owner,
            "internal" => global::BedrockProtocol.Enums.CommandPermissionLevel.Internal,
            _ => throw new InvalidOperationException($"Unknown CommandPermissionLevel wire value: {enumText6}"),
        };
        AliasEnum = reader.ReadInt32(true);
        int count10 = checked((int)reader.ReadVarUInt());
        CommandDataChainedSubcommandIndexes = new List<uint>(count10);
        for (int i10 = 0; i10 < count10; i10++) {
            uint item10 = default!;
            item10 = reader.ReadUInt32(true);
            CommandDataChainedSubcommandIndexes.Add(item10);
        }
        int count12 = checked((int)reader.ReadVarUInt());
        Overloads = new List<AvailableCommandsPacketOverloadData>(count12);
        for (int i12 = 0; i12 < count12; i12++) {
            AvailableCommandsPacketOverloadData item12 = default!;
            AvailableCommandsPacketOverloadData readValue1012 = new();
            readValue1012.Read(reader);
            item12 = readValue1012;
            Overloads.Add(item12);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteVarString(Description);
        writer.WriteUInt16(Flags, true);
        writer.WriteVarString(PermissionLevel switch {
            global::BedrockProtocol.Enums.CommandPermissionLevel.Any => "any",
            global::BedrockProtocol.Enums.CommandPermissionLevel.GameDirectors => "gamedirectors",
            global::BedrockProtocol.Enums.CommandPermissionLevel.Admin => "admin",
            global::BedrockProtocol.Enums.CommandPermissionLevel.Host => "host",
            global::BedrockProtocol.Enums.CommandPermissionLevel.Owner => "owner",
            global::BedrockProtocol.Enums.CommandPermissionLevel.Internal => "internal",
            _ => throw new InvalidOperationException($"Unknown CommandPermissionLevel value: {PermissionLevel}"),
        });
        writer.WriteInt32(AliasEnum, true);
        writer.WriteVarUInt(checked((uint)CommandDataChainedSubcommandIndexes.Count));
        foreach (var item11 in CommandDataChainedSubcommandIndexes) {
            writer.WriteUInt32(item11, true);
        }
        writer.WriteVarUInt(checked((uint)Overloads.Count));
        foreach (var item13 in Overloads) {
            item13.Write(writer);
        }
    }
}

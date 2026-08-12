#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class EducationLevelSettings {
    public string CodeBuilderDefaultURI = string.Empty;
    public string CodeBuilderTitle = string.Empty;
    public bool CanResizeCodeBuilder;
    public bool DisableLegacyTitleBar;
    public string PostProcessFilter = string.Empty;
    public string ScreenshotBorderResourcePath = string.Empty;
    public AgentCapabilities AgentCapabilities = new();
    public EducationLocalLevelSettings LocalSettings = new();
    public bool DeprecatedAlwaysFalse;
    public ExternalLinkSettings ExternalLinkSettings = new();

    public void Read(BinaryReader reader) {
        CodeBuilderDefaultURI = reader.ReadVarString();
        CodeBuilderTitle = reader.ReadVarString();
        CanResizeCodeBuilder = reader.ReadBool();
        DisableLegacyTitleBar = reader.ReadBool();
        PostProcessFilter = reader.ReadVarString();
        ScreenshotBorderResourcePath = reader.ReadVarString();
        AgentCapabilities.Read(reader);
        LocalSettings.Read(reader);
        DeprecatedAlwaysFalse = reader.ReadBool();
        ExternalLinkSettings.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(CodeBuilderDefaultURI);
        writer.WriteVarString(CodeBuilderTitle);
        writer.WriteBool(CanResizeCodeBuilder);
        writer.WriteBool(DisableLegacyTitleBar);
        writer.WriteVarString(PostProcessFilter);
        writer.WriteVarString(ScreenshotBorderResourcePath);
        AgentCapabilities.Write(writer);
        LocalSettings.Write(writer);
        writer.WriteBool(DeprecatedAlwaysFalse);
        ExternalLinkSettings.Write(writer);
    }
}

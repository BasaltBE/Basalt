using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record ResourcePackStackPacket : DataPacket
{
    public bool MustAccept { get; set; }
    public List<ResourcePackStackEntry> Packs { get; set; } = [];
    public string BaseGameVersion { get; set; } = string.Empty;
    public List<ExperimentData> Experiments { get; set; } = [];
    public bool ExperimentsPreviouslyToggled { get; set; }
    public bool IncludeEditorPacks { get; set; }

    public override PacketId PacketId => PacketId.ResourcePackStack;

    public override void Deserialize(ref BinaryReader reader)
    {
        MustAccept = reader.ReadBool();
        int packsLength = checked((int)reader.ReadVarUInt());
        Packs = new List<ResourcePackStackEntry>(packsLength);
        for (int i = 0; i < packsLength; i++)
        {
            ResourcePackStackEntry pack = new();
            pack.Read(ref reader);
            Packs.Add(pack);
        }
        BaseGameVersion = reader.ReadVarString();
        int experimentsLength = checked((int)reader.ReadUInt32(true));
        Experiments = new List<ExperimentData>(experimentsLength);
        for (int i = 0; i < experimentsLength; i++)
        {
            ExperimentData experiment = new();
            experiment.Read(ref reader);
            Experiments.Add(experiment);
        }
        ExperimentsPreviouslyToggled = reader.ReadBool();
        IncludeEditorPacks = reader.ReadBool();
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteBool(MustAccept);
        writer.WriteVarUInt((uint)Packs.Count);
        for (int i = 0; i < Packs.Count; i++)
        {
            Packs[i].Write(ref writer);
        }
        writer.WriteVarString(BaseGameVersion);
        writer.WriteUInt32((uint)Experiments.Count, true);
        for (int i = 0; i < Experiments.Count; i++)
        {
            Experiments[i].Write(ref writer);
        }
        writer.WriteBool(ExperimentsPreviouslyToggled);
        writer.WriteBool(IncludeEditorPacks);
    }
}


using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ResourcePackStack)]
public sealed record ResourcePackStackPacket : DataPacket
{
    /// <summary>
    /// Whether the client must accept the resource packs.
    /// </summary>
    public bool MustAccept;

    /// <summary>
    /// List of resource packs that the client must accept.
    /// </summary>
    public List<ResourcePackStackEntry> Packs = [];

    /// <summary>
    /// The base game version of the client.
    /// </summary>
    public string BaseGameVersion = string.Empty;

    /// <summary>
    /// List of experiments that the server has enabled.
    /// </summary>
    public List<ExperimentData> Experiments = [];

    /// <summary>
    /// Whether the client has previously toggled any experiments.
    /// </summary>
    public bool ExperimentsPreviouslyToggled;

    /// <summary>
    /// Whether the server includes editor packs.
    /// </summary>
    public bool IncludeEditorPacks;

    public override void Deserialize(BinaryReader reader)
    {
        MustAccept = reader.ReadBool();
        int packsLength = checked((int)reader.ReadVarUInt());
        Packs = new List<ResourcePackStackEntry>(packsLength);
        for (int i = 0; i < packsLength; i++)
        {
            ResourcePackStackEntry pack = new();
            pack.Read(reader);
            Packs.Add(pack);
        }
        BaseGameVersion = reader.ReadVarString();
        int experimentsLength = checked((int)reader.ReadUInt32(true));
        Experiments = new List<ExperimentData>(experimentsLength);
        for (int i = 0; i < experimentsLength; i++)
        {
            ExperimentData experiment = new();
            experiment.Read(reader);
            Experiments.Add(experiment);
        }
        ExperimentsPreviouslyToggled = reader.ReadBool();
        IncludeEditorPacks = reader.ReadBool();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteBool(MustAccept);
        writer.WriteVarUInt((uint)Packs.Count);
        for (int i = 0; i < Packs.Count; i++)
        {
            Packs[i].Write(writer);
        }
        writer.WriteVarString(BaseGameVersion);
        writer.WriteUInt32((uint)Experiments.Count, true);
        for (int i = 0; i < Experiments.Count; i++)
        {
            Experiments[i].Write(writer);
        }
        writer.WriteBool(ExperimentsPreviouslyToggled);
        writer.WriteBool(IncludeEditorPacks);
    }
}


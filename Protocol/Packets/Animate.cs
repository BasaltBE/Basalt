using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.Animate)]
public sealed record AnimatePacket : DataPacket {
    public AnimateActionType ActionType;
    public ulong EntityRuntimeId;
    public float Data;
    public AnimateSwingSource? SwingSource;

    public override void Deserialize(Binary.BinaryReader reader) {
        ActionType = (AnimateActionType)reader.ReadUInt8();
        EntityRuntimeId = reader.ReadVarULong();
        Data = reader.ReadF32(true);
        SwingSource = reader.ReadBool()
            ? SwingSourceFromString(reader.ReadVarString())
            : null;
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteUInt8((byte)ActionType);
        writer.WriteVarULong(EntityRuntimeId);
        writer.WriteF32(Data, true);
        writer.WriteBool(SwingSource.HasValue);
        if (SwingSource.HasValue) {
            writer.WriteVarString(SwingSourceToString(SwingSource.Value));
        }
    }

    private static AnimateSwingSource SwingSourceFromString(string source) => source switch {
        "none" => AnimateSwingSource.None,
        "build" => AnimateSwingSource.Build,
        "mine" => AnimateSwingSource.Mine,
        "interact" => AnimateSwingSource.Interact,
        "attack" => AnimateSwingSource.Attack,
        "useitem" => AnimateSwingSource.UseItem,
        "throwitem" => AnimateSwingSource.ThrowItem,
        "dropitem" => AnimateSwingSource.DropItem,
        "event" => AnimateSwingSource.Event,
        _ => throw new InvalidOperationException($"Unknown animate swing source: {source}.")
    };

    private static string SwingSourceToString(AnimateSwingSource source) => source switch {
        AnimateSwingSource.None => "none",
        AnimateSwingSource.Build => "build",
        AnimateSwingSource.Mine => "mine",
        AnimateSwingSource.Interact => "interact",
        AnimateSwingSource.Attack => "attack",
        AnimateSwingSource.UseItem => "useitem",
        AnimateSwingSource.ThrowItem => "throwitem",
        AnimateSwingSource.DropItem => "dropitem",
        AnimateSwingSource.Event => "event",
        _ => throw new InvalidOperationException($"Unknown animate swing source: {source}.")
    };
}

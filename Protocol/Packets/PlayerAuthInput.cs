using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.PlayerAuthInput)]
public sealed record PlayerAuthInputPacket : DataPacket
{
    /// <summary>
    /// Player camera pitch.
    /// </summary>
    public float Pitch;

    /// <summary>
    /// Player camera yaw.
    /// </summary>
    public float Yaw;

    /// <summary>
    /// Reported player position.
    /// </summary>
    public Vec3f Position;

    /// <summary>
    /// Movement input vector.
    /// </summary>
    public Vec2f MoveVector;

    /// <summary>
    /// Player head yaw.
    /// </summary>
    public float HeadYaw;

    /// <summary>
    /// Input bitset flags for this tick.
    /// </summary>
    public PlayerAuthInputData InputData;

    /// <summary>
    /// Active input mode.
    /// </summary>
    public InputMode InputMode;

    /// <summary>
    /// Active play mode.
    /// </summary>
    public PlayMode PlayMode;

    /// <summary>
    /// Active interaction model.
    /// </summary>
    public InteractionModel InteractionModel;

    /// <summary>
    /// Interaction pitch value.
    /// </summary>
    public float InteractPitch;

    /// <summary>
    /// Interaction yaw value.
    /// </summary>
    public float InteractYaw;

    /// <summary>
    /// Server tick when sent.
    /// </summary>
    public ulong Tick;

    /// <summary>
    /// Position delta.
    /// </summary>
    public Vec3f Delta;

    /// <summary>
    /// Item interaction transaction data.
    /// </summary>
    public UseItemTransactionData ItemInteractionData = new();

    /// <summary>
    /// Item stack request payload.
    /// </summary>
    public ItemStackRequest ItemStackRequest = new();

    /// <summary>
    /// Player block action entries.
    /// Contains block breaking actions
    /// </summary>
    public List<PlayerBlockAction> BlockActions = [];

    /// <summary>
    /// Vehicle rotation when predicted vehicle flag is set.
    /// </summary>
    public Vec2f VehicleRotation;

    /// <summary>
    /// Predicted vehicle runtime id.
    /// </summary>
    public long ClientPredictedVehicle;

    /// <summary>
    /// Analogue movement vector.
    /// </summary>
    public Vec2f AnalogueMoveVector;

    /// <summary>
    /// Camera forward orientation vector.
    /// </summary>
    public Vec3f CameraOrientation;

    /// <summary>
    /// Raw movement vector before modifiers.
    /// </summary>
    public Vec2f RawMoveVector;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        Pitch = reader.ReadF32(true);
        Yaw = reader.ReadF32(true);
        Position.Read(reader);
        MoveVector.Read(reader);
        HeadYaw = reader.ReadF32(true);

        InputData.Read(reader);
        InputMode = (InputMode)reader.ReadVarUInt();
        PlayMode = (PlayMode)reader.ReadVarUInt();
        InteractionModel = (InteractionModel)reader.ReadVarUInt();
        InteractPitch = reader.ReadF32(true);
        InteractYaw = reader.ReadF32(true);
        Tick = reader.ReadVarULong();
        Delta.Read(reader);

        if (InputData.HasFlag(PlayerAuthInputFlag.PerformItemInteraction))
        {
            ItemInteractionData.Read(reader);
        }

        if (InputData.HasFlag(PlayerAuthInputFlag.PerformItemStackRequest))
        {
            ItemStackRequest.Read(reader);
        }

        if (InputData.HasFlag(PlayerAuthInputFlag.PerformBlockActions))
        {
            int count = checked((int)reader.ReadZigZag());
            BlockActions = new(count);
            for (int i = 0; i < count; i++)
            {
                PlayerBlockAction action = new();
                action.Read(reader);
                BlockActions.Add(action);
            }
        }

        if (InputData.HasFlag(PlayerAuthInputFlag.ClientPredictedVehicle))
        {
            VehicleRotation.Read(reader);
            ClientPredictedVehicle = reader.ReadZigZong();
        }

        AnalogueMoveVector.Read(reader);
        CameraOrientation.Read(reader);
        RawMoveVector.Read(reader);
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteF32(Pitch, true);
        writer.WriteF32(Yaw, true);
        Position.Write(writer);
        MoveVector.Write(writer);
        writer.WriteF32(HeadYaw, true);

        InputData.Write(writer);
        writer.WriteVarUInt((uint)InputMode);
        writer.WriteVarUInt((uint)PlayMode);
        writer.WriteVarUInt((uint)InteractionModel);
        writer.WriteF32(InteractPitch, true);
        writer.WriteF32(InteractYaw, true);
        writer.WriteVarULong(Tick);
        Delta.Write(writer);

        if (InputData.HasFlag(PlayerAuthInputFlag.PerformItemInteraction))
        {
            ItemInteractionData.Write(writer);
        }

        if (InputData.HasFlag(PlayerAuthInputFlag.PerformItemStackRequest))
        {
            ItemStackRequest.Write(writer);
        }

        if (InputData.HasFlag(PlayerAuthInputFlag.PerformBlockActions))
        {
            writer.WriteZigZag(BlockActions.Count);
            for (int i = 0; i < BlockActions.Count; i++)
            {
                BlockActions[i].Write(writer);
            }
        }

        if (InputData.HasFlag(PlayerAuthInputFlag.ClientPredictedVehicle))
        {
            VehicleRotation.Write(writer);
            writer.WriteZigZong(ClientPredictedVehicle);
        }

        AnalogueMoveVector.Write(writer);
        CameraOrientation.Write(writer);
        RawMoveVector.Write(writer);
    }
}

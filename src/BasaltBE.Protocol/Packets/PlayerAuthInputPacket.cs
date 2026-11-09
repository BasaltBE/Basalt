using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(144)]
public sealed class PlayerAuthInputPacket : DataPacket {
    const uint MaximumInputCount = 100;

    public Vec2 Rotation = new();
    public Vec3 Position = new();
    public Vec2 MoveVector = new();
    public float HeadRotation;
    public PlayerAuthInputData[] InputData = [];
    public InputMode InputMode;
    public ClientPlayMode PlayMode;
    public NewInteractionModel InteractionModel;
    public Vec2 InteractRotation = new();
    public PlayerInputTick ClientTick = new();
    public Vec3 PositionDelta = new();
    public PackedItemUseLegacyInventoryTransaction? ItemUseTransaction;
    public ItemStackRequestData? ItemStackRequest;
    public PlayerBlockActionData[]? PlayerBlockActions;
    public Vec2? VehicleRotation;
    public long? ClientPredictedVehicle;
    public Vec2 AnalogMoveVector = new();
    public Vec3 CameraOrientation = new();
    public Vec2 RawMoveVector = new();

    public override void Deserialize(ref BinaryReader reader) {
        Rotation.Read(ref reader);
        Position.Read(ref reader);
        MoveVector.Read(ref reader);
        HeadRotation = reader.ReadF32(true);

        uint inputCount = reader.ReadVarUInt();
        if (inputCount > MaximumInputCount)
            throw new InvalidDataException("Player auth input contains too many input flags.");
        InputData = new PlayerAuthInputData[(int)inputCount];
        for (int i = 0; i < InputData.Length; i++)
            InputData[i] = (PlayerAuthInputData)reader.ReadZigZag();

        InputMode = (InputMode)reader.ReadVarUInt();
        PlayMode = (ClientPlayMode)reader.ReadVarUInt();
        InteractionModel = (NewInteractionModel)reader.ReadZigZag();
        InteractRotation.Read(ref reader);
        ClientTick.Read(ref reader);
        PositionDelta.Read(ref reader);

        ItemUseTransaction = reader.ReadBool() ? new PackedItemUseLegacyInventoryTransaction() : null;
        ItemUseTransaction?.Read(ref reader);
        ItemStackRequest = reader.ReadBool() ? new ItemStackRequestData() : null;
        ItemStackRequest?.Read(ref reader);

        if (reader.ReadBool()) {
            uint actionCount = reader.ReadVarUInt();
            if (actionCount > MaximumInputCount)
                throw new InvalidDataException("Player auth input contains too many block actions.");
            PlayerBlockActions = new PlayerBlockActionData[(int)actionCount];
            for (int i = 0; i < PlayerBlockActions.Length; i++) {
                PlayerBlockActions[i] = new PlayerBlockActionData();
                PlayerBlockActions[i].Read(ref reader);
            }
        }
        else {
            PlayerBlockActions = null;
        }

        VehicleRotation = reader.ReadBool() ? new Vec2() : null;
        VehicleRotation?.Read(ref reader);
        ClientPredictedVehicle = reader.ReadBool() ? reader.ReadZigZong() : null;
        AnalogMoveVector.Read(ref reader);
        CameraOrientation.Read(ref reader);
        RawMoveVector.Read(ref reader);
    }

    public override void Serialize(ref BinaryWriter writer) {
        ArgumentNullException.ThrowIfNull(Rotation);
        ArgumentNullException.ThrowIfNull(Position);
        ArgumentNullException.ThrowIfNull(MoveVector);
        ArgumentNullException.ThrowIfNull(InteractRotation);
        ArgumentNullException.ThrowIfNull(ClientTick);
        ArgumentNullException.ThrowIfNull(PositionDelta);
        ArgumentNullException.ThrowIfNull(AnalogMoveVector);
        ArgumentNullException.ThrowIfNull(CameraOrientation);
        ArgumentNullException.ThrowIfNull(RawMoveVector);
        if (InputData.Length > MaximumInputCount)
            throw new InvalidDataException("Player auth input contains too many input flags.");
        if (PlayerBlockActions?.Length > MaximumInputCount)
            throw new InvalidDataException("Player auth input contains too many block actions.");

        Rotation.Write(ref writer);
        Position.Write(ref writer);
        MoveVector.Write(ref writer);
        writer.WriteF32(HeadRotation, true);
        writer.WriteVarUInt((uint)InputData.Length);
        foreach (PlayerAuthInputData input in InputData)
            writer.WriteZigZag((int)input);
        writer.WriteVarUInt((uint)InputMode);
        writer.WriteVarUInt((uint)PlayMode);
        writer.WriteZigZag((int)InteractionModel);
        InteractRotation.Write(ref writer);
        ClientTick.Write(ref writer);
        PositionDelta.Write(ref writer);

        writer.WriteBool(ItemUseTransaction is not null);
        ItemUseTransaction?.Write(ref writer);
        writer.WriteBool(ItemStackRequest is not null);
        ItemStackRequest?.Write(ref writer);

        writer.WriteBool(PlayerBlockActions is not null);
        if (PlayerBlockActions is not null) {
            writer.WriteVarUInt((uint)PlayerBlockActions.Length);
            foreach (PlayerBlockActionData action in PlayerBlockActions) {
                ArgumentNullException.ThrowIfNull(action);
                action.Write(ref writer);
            }
        }

        writer.WriteBool(VehicleRotation is not null);
        VehicleRotation?.Write(ref writer);
        writer.WriteBool(ClientPredictedVehicle is not null);
        if (ClientPredictedVehicle is long vehicleId)
            writer.WriteZigZong(vehicleId);
        AnalogMoveVector.Write(ref writer);
        CameraOrientation.Write(ref writer);
        RawMoveVector.Write(ref writer);
    }
}

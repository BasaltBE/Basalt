using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(144)]
public sealed class PlayerAuthInputPacket : DataPacket {
    public Vec2 PlayerRotation = new();
    public Vec3 Position = new();
    public Vec2 MoveVector = new();
    public float PlayerHeadRotation;
    public PlayerAuthInputData[] InputData = [];
    public InputMode InputMode;
    public ClientPlayMode PlayMode;
    public NewInteractionModel NewInteractionModel;
    public Vec2 InteractRotation = new();
    public ulong ClientTick;
    public Vec3 PosDelta = new();
    public PackedItemUseLegacyInventoryTransactionData? ItemUseTransaction;
    public ItemStackRequestData? ItemStackRequest;
    public PlayerBlockActionData[]? PlayerBlockActions;
    public Vec2? VehicleRotation;
    public long? ClientPredictedVehicle;
    public Vec2 AnalogMoveVector = new();
    public Vec3 CameraOrientation = new();
    public Vec2 RawMoveVector = new();

    public override void Serialize(ref BinaryWriter writer) {
        PlayerRotation.Write(ref writer);
        Position.Write(ref writer);
        MoveVector.Write(ref writer);
        writer.WriteF32(PlayerHeadRotation, true);
        writer.WriteVarUInt((uint)InputData.Length);
        foreach (PlayerAuthInputData input in InputData) writer.WriteZigZag((int)input);
        writer.WriteVarUInt((uint)InputMode);
        writer.WriteVarUInt((uint)PlayMode);
        writer.WriteZigZag((int)NewInteractionModel);
        InteractRotation.Write(ref writer);
        writer.WriteVarULong(ClientTick);
        PosDelta.Write(ref writer);
        writer.WriteBool(ItemUseTransaction is not null);
        if (ItemUseTransaction is not null) ItemUseTransaction.Write(ref writer);
        writer.WriteBool(ItemStackRequest is not null);
        if (ItemStackRequest is not null) ItemStackRequest.Write(ref writer);
        writer.WriteBool(PlayerBlockActions is not null);
        if (PlayerBlockActions is not null) {
            writer.WriteVarUInt((uint)PlayerBlockActions.Length);
            foreach (PlayerBlockActionData action in PlayerBlockActions) action.Write(ref writer);
        }
        writer.WriteBool(VehicleRotation is not null);
        if (VehicleRotation is not null) VehicleRotation.Write(ref writer);
        writer.WriteBool(ClientPredictedVehicle.HasValue);
        if (ClientPredictedVehicle.HasValue) writer.WriteVarLong(ClientPredictedVehicle.Value);
        AnalogMoveVector.Write(ref writer);
        CameraOrientation.Write(ref writer);
        RawMoveVector.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        PlayerRotation.Read(ref reader);
        Position.Read(ref reader);
        MoveVector.Read(ref reader);
        PlayerHeadRotation = reader.ReadF32(true);
        InputData = new PlayerAuthInputData[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < InputData.Length; index++) InputData[index] = (PlayerAuthInputData)reader.ReadZigZag();
        InputMode = (InputMode)reader.ReadVarUInt();
        PlayMode = (ClientPlayMode)reader.ReadVarUInt();
        NewInteractionModel = (NewInteractionModel)reader.ReadZigZag();
        InteractRotation.Read(ref reader);
        ClientTick = reader.ReadVarULong();
        PosDelta.Read(ref reader);
        ItemUseTransaction = reader.ReadBool() ? new PackedItemUseLegacyInventoryTransactionData() : null;
        if (ItemUseTransaction is not null) ItemUseTransaction.Read(ref reader);
        ItemStackRequest = reader.ReadBool() ? new ItemStackRequestData() : null;
        if (ItemStackRequest is not null) ItemStackRequest.Read(ref reader);
        PlayerBlockActions = reader.ReadBool() ? new PlayerBlockActionData[checked((int)reader.ReadVarUInt())] : null;
        if (PlayerBlockActions is not null) {
            for (int index = 0; index < PlayerBlockActions.Length; index++) {
                PlayerBlockActionData action = new();
                action.Read(ref reader);
                PlayerBlockActions[index] = action;
            }
        }
        VehicleRotation = reader.ReadBool() ? new Vec2() : null;
        if (VehicleRotation is not null) VehicleRotation.Read(ref reader);
        ClientPredictedVehicle = reader.ReadBool() ? reader.ReadVarLong() : null;
        AnalogMoveVector.Read(ref reader);
        CameraOrientation.Read(ref reader);
        RawMoveVector.Read(ref reader);
    }
}

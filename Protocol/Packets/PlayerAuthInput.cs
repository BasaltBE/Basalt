using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record PlayerAuthInputPacket : DataPacket
{
    public float Pitch { get; set; }
    public float Yaw { get; set; }
    public Vec3f Position { get; set; }
    public Vec2f MoveVector { get; set; }
    public float HeadYaw { get; set; }
    public PlayerAuthInputData InputData { get; set; }
    public InputMode InputMode { get; set; }
    public PlayMode PlayMode { get; set; }
    public InteractionModel InteractionModel { get; set; }
    public float InteractPitch { get; set; }
    public float InteractYaw { get; set; }
    public ulong Tick { get; set; }
    public Vec3f Delta { get; set; }
    public UseItemTransactionData ItemInteractionData { get; set; } = new();
    public ItemStackRequest ItemStackRequest { get; set; } = new();
    public List<PlayerBlockAction> BlockActions { get; set; } = [];
    public Vec2f VehicleRotation { get; set; }
    public long ClientPredictedVehicle { get; set; }
    public Vec2f AnalogueMoveVector { get; set; }
    public Vec3f CameraOrientation { get; set; }
    public Vec2f RawMoveVector { get; set; }

    public override PacketId PacketId => PacketId.PlayerAuthInput;

    public bool HasFlag(PlayerAuthInputFlag flag)
    {
        return InputData.HasFlag(flag);
    }

    public override void Deserialize(ref BinaryReader reader)
    {
        Pitch = reader.ReadF32(true);
        Yaw = reader.ReadF32(true);
        Vec3f position = Position;
        position.Read(ref reader);
        Position = position;
        Vec2f moveVector = MoveVector;
        moveVector.Read(ref reader);
        MoveVector = moveVector;
        HeadYaw = reader.ReadF32(true);

        InputData.Read(ref reader);

        InputMode = (InputMode)reader.ReadVarUInt();
        PlayMode = (PlayMode)reader.ReadVarUInt();
        InteractionModel = (InteractionModel)reader.ReadVarUInt();
        InteractPitch = reader.ReadF32(true);
        InteractYaw = reader.ReadF32(true);
        Tick = reader.ReadVarULong();
        Vec3f delta = Delta;
        delta.Read(ref reader);
        Delta = delta;

        if (HasFlag(PlayerAuthInputFlag.PerformItemInteraction))
        {
            ItemInteractionData.Read(ref reader);
        }

        if (HasFlag(PlayerAuthInputFlag.PerformItemStackRequest))
        {
            ItemStackRequest.Read(ref reader);
        }

        if (HasFlag(PlayerAuthInputFlag.PerformBlockActions))
        {
            int blockActionCount = checked((int)reader.ReadZigZag());
            BlockActions = new(blockActionCount);
            for (int i = 0; i < blockActionCount; i++)
            {
                PlayerBlockAction action = new();
                action.Read(ref reader);
                BlockActions.Add(action);
            }
        }

        if (HasFlag(PlayerAuthInputFlag.ClientPredictedVehicle))
        {
            Vec2f vehicleRotation = VehicleRotation;
            vehicleRotation.Read(ref reader);
            VehicleRotation = vehicleRotation;
            ClientPredictedVehicle = reader.ReadZigZong();
        }

        Vec2f analogueMoveVector = AnalogueMoveVector;
        analogueMoveVector.Read(ref reader);
        AnalogueMoveVector = analogueMoveVector;
        Vec3f cameraOrientation = CameraOrientation;
        cameraOrientation.Read(ref reader);
        CameraOrientation = cameraOrientation;
        Vec2f rawMoveVector = RawMoveVector;
        rawMoveVector.Read(ref reader);
        RawMoveVector = rawMoveVector;
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteF32(Pitch, true);
        writer.WriteF32(Yaw, true);
        Position.Write(ref writer);
        MoveVector.Write(ref writer);
        writer.WriteF32(HeadYaw, true);

        InputData.Write(ref writer);

        writer.WriteVarUInt((uint)InputMode);
        writer.WriteVarUInt((uint)PlayMode);
        writer.WriteVarUInt((uint)InteractionModel);
        writer.WriteF32(InteractPitch, true);
        writer.WriteF32(InteractYaw, true);
        writer.WriteVarULong(Tick);
        Delta.Write(ref writer);

        if (HasFlag(PlayerAuthInputFlag.PerformItemInteraction))
        {
            ItemInteractionData.Write(ref writer);
        }

        if (HasFlag(PlayerAuthInputFlag.PerformItemStackRequest))
        {
            ItemStackRequest.Write(ref writer);
        }

        if (HasFlag(PlayerAuthInputFlag.PerformBlockActions))
        {
            writer.WriteZigZag(BlockActions.Count);
            for (int i = 0; i < BlockActions.Count; i++)
            {
                BlockActions[i].Write(ref writer);
            }
        }

        if (HasFlag(PlayerAuthInputFlag.ClientPredictedVehicle))
        {
            VehicleRotation.Write(ref writer);
            writer.WriteZigZong(ClientPredictedVehicle);
        }

        AnalogueMoveVector.Write(ref writer);
        CameraOrientation.Write(ref writer);
        RawMoveVector.Write(ref writer);
    }

}

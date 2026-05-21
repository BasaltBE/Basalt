using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class PlayerBlockAction : DataType
{
    /// <summary>
    /// Player action type for this block action.
    /// </summary>
    public PlayerActionType Action;

    /// <summary>
    /// Target block position.
    /// </summary>
    public BlockPos BlockPos;

    /// <summary>
    /// Block face index.
    /// </summary>
    public int Face;

    public void Read(BinaryReader reader)
    {
        Action = (PlayerActionType)reader.ReadZigZag();
        switch (Action)
        {
            case PlayerActionType.StartDestroyBlock:
            case PlayerActionType.AbortDestroyBlock:
            case PlayerActionType.CrackBlock:
            case PlayerActionType.PredictDestroyBlock:
            case PlayerActionType.ContinueDestroyBlock:
                BlockPos.Read(reader);
                Face = reader.ReadZigZag();
                break;
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteZigZag((int)Action);
        switch (Action)
        {
            case PlayerActionType.StartDestroyBlock:
            case PlayerActionType.AbortDestroyBlock:
            case PlayerActionType.CrackBlock:
            case PlayerActionType.PredictDestroyBlock:
            case PlayerActionType.ContinueDestroyBlock:
                BlockPos.Write(writer);
                writer.WriteZigZag(Face);
                break;
        }
    }
}

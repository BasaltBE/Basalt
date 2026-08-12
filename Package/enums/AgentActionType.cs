#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum AgentActionType {
    Attack = 1,
    Collect = 2,
    Destroy = 3,
    DetectRedstone = 4,
    DetectObstacle = 5,
    Drop = 6,
    DropAll = 7,
    Inspect = 8,
    InspectData = 9,
    InspectItemCount = 10,
    InspectItemDetail = 11,
    InspectItemSpace = 12,
    Interact = 13,
    Move = 14,
    PlaceBlock = 15,
    Till = 16,
    TransferItemTo = 17,
    Turn = 18,
}

public static class AgentActionTypeExtensions {
    public static string ToProtoString(this AgentActionType value) => value.ToProtocolString();

    public static string ToProtocolString(this AgentActionType value) {
        return value switch {
            AgentActionType.Attack => "Attack",
            AgentActionType.Collect => "Collect",
            AgentActionType.Destroy => "Destroy",
            AgentActionType.DetectRedstone => "DetectRedstone",
            AgentActionType.DetectObstacle => "DetectObstacle",
            AgentActionType.Drop => "Drop",
            AgentActionType.DropAll => "DropAll",
            AgentActionType.Inspect => "Inspect",
            AgentActionType.InspectData => "InspectData",
            AgentActionType.InspectItemCount => "InspectItemCount",
            AgentActionType.InspectItemDetail => "InspectItemDetail",
            AgentActionType.InspectItemSpace => "InspectItemSpace",
            AgentActionType.Interact => "Interact",
            AgentActionType.Move => "Move",
            AgentActionType.PlaceBlock => "PlaceBlock",
            AgentActionType.Till => "Till",
            AgentActionType.TransferItemTo => "TransferItemTo",
            AgentActionType.Turn => "Turn",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown AgentActionType value.")
        };
    }

    public static AgentActionType FromProtocolString(string value) {
        return value switch {
            "Attack" => AgentActionType.Attack,
            "Collect" => AgentActionType.Collect,
            "Destroy" => AgentActionType.Destroy,
            "DetectRedstone" => AgentActionType.DetectRedstone,
            "DetectObstacle" => AgentActionType.DetectObstacle,
            "Drop" => AgentActionType.Drop,
            "DropAll" => AgentActionType.DropAll,
            "Inspect" => AgentActionType.Inspect,
            "InspectData" => AgentActionType.InspectData,
            "InspectItemCount" => AgentActionType.InspectItemCount,
            "InspectItemDetail" => AgentActionType.InspectItemDetail,
            "InspectItemSpace" => AgentActionType.InspectItemSpace,
            "Interact" => AgentActionType.Interact,
            "Move" => AgentActionType.Move,
            "PlaceBlock" => AgentActionType.PlaceBlock,
            "Till" => AgentActionType.Till,
            "TransferItemTo" => AgentActionType.TransferItemTo,
            "Turn" => AgentActionType.Turn,
            _ => throw new ArgumentException($"Unknown AgentActionType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out AgentActionType result) {
        switch (value) {
            case "Attack":
                result = AgentActionType.Attack;
                return true;
            case "Collect":
                result = AgentActionType.Collect;
                return true;
            case "Destroy":
                result = AgentActionType.Destroy;
                return true;
            case "DetectRedstone":
                result = AgentActionType.DetectRedstone;
                return true;
            case "DetectObstacle":
                result = AgentActionType.DetectObstacle;
                return true;
            case "Drop":
                result = AgentActionType.Drop;
                return true;
            case "DropAll":
                result = AgentActionType.DropAll;
                return true;
            case "Inspect":
                result = AgentActionType.Inspect;
                return true;
            case "InspectData":
                result = AgentActionType.InspectData;
                return true;
            case "InspectItemCount":
                result = AgentActionType.InspectItemCount;
                return true;
            case "InspectItemDetail":
                result = AgentActionType.InspectItemDetail;
                return true;
            case "InspectItemSpace":
                result = AgentActionType.InspectItemSpace;
                return true;
            case "Interact":
                result = AgentActionType.Interact;
                return true;
            case "Move":
                result = AgentActionType.Move;
                return true;
            case "PlaceBlock":
                result = AgentActionType.PlaceBlock;
                return true;
            case "Till":
                result = AgentActionType.Till;
                return true;
            case "TransferItemTo":
                result = AgentActionType.TransferItemTo;
                return true;
            case "Turn":
                result = AgentActionType.Turn;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

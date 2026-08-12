#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum PlayerActionType {
    Unknown = -1,
    StartDestroyBlock = 0,
    AbortDestroyBlock = 1,
    StopDestroyBlock = 2,
    StartSleeping = 5,
    StopSleeping = 6,
    Respawn = 7,
    StartJump = 8,
    StartSprinting = 9,
    StopSprinting = 10,
    StartSneaking = 11,
    StopSneaking = 12,
    CreativeDestroyBlock = 13,
    ChangeDimensionAck = 14,
    StartGliding = 15,
    StopGliding = 16,
    DenyDestroyBlock = 17,
    CrackBlock = 18,
    StartSwimming = 21,
    StopSwimming = 22,
    StartSpinAttack = 23,
    StopSpinAttack = 24,
    PredictDestroyBlock = 26,
    ContinueDestroyBlock = 27,
    StartItemUseOn = 28,
    StopItemUseOn = 29,
    HandledTeleport = 30,
    MissedSwing = 31,
    StartCrawling = 32,
    StopCrawling = 33,
    StartFlying = 34,
    StopFlying = 35,
    StartUsingItem = 37,
    InternalUpdate = 38,
    Count = 39,
}

public static class PlayerActionTypeExtensions {
    public static string ToProtoString(this PlayerActionType value) => value.ToProtocolString();

    public static string ToProtocolString(this PlayerActionType value) {
        return value switch {
            PlayerActionType.Unknown => "Unknown",
            PlayerActionType.StartDestroyBlock => "StartDestroyBlock",
            PlayerActionType.AbortDestroyBlock => "AbortDestroyBlock",
            PlayerActionType.StopDestroyBlock => "StopDestroyBlock",
            PlayerActionType.StartSleeping => "StartSleeping",
            PlayerActionType.StopSleeping => "StopSleeping",
            PlayerActionType.Respawn => "Respawn",
            PlayerActionType.StartJump => "StartJump",
            PlayerActionType.StartSprinting => "StartSprinting",
            PlayerActionType.StopSprinting => "StopSprinting",
            PlayerActionType.StartSneaking => "StartSneaking",
            PlayerActionType.StopSneaking => "StopSneaking",
            PlayerActionType.CreativeDestroyBlock => "CreativeDestroyBlock",
            PlayerActionType.ChangeDimensionAck => "ChangeDimensionAck",
            PlayerActionType.StartGliding => "StartGliding",
            PlayerActionType.StopGliding => "StopGliding",
            PlayerActionType.DenyDestroyBlock => "DenyDestroyBlock",
            PlayerActionType.CrackBlock => "CrackBlock",
            PlayerActionType.StartSwimming => "StartSwimming",
            PlayerActionType.StopSwimming => "StopSwimming",
            PlayerActionType.StartSpinAttack => "StartSpinAttack",
            PlayerActionType.StopSpinAttack => "StopSpinAttack",
            PlayerActionType.PredictDestroyBlock => "PredictDestroyBlock",
            PlayerActionType.ContinueDestroyBlock => "ContinueDestroyBlock",
            PlayerActionType.StartItemUseOn => "StartItemUseOn",
            PlayerActionType.StopItemUseOn => "StopItemUseOn",
            PlayerActionType.HandledTeleport => "HandledTeleport",
            PlayerActionType.MissedSwing => "MissedSwing",
            PlayerActionType.StartCrawling => "StartCrawling",
            PlayerActionType.StopCrawling => "StopCrawling",
            PlayerActionType.StartFlying => "StartFlying",
            PlayerActionType.StopFlying => "StopFlying",
            PlayerActionType.StartUsingItem => "StartUsingItem",
            PlayerActionType.InternalUpdate => "InternalUpdate",
            PlayerActionType.Count => "Count",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PlayerActionType value.")
        };
    }

    public static PlayerActionType FromProtocolString(string value) {
        return value switch {
            "Unknown" => PlayerActionType.Unknown,
            "StartDestroyBlock" => PlayerActionType.StartDestroyBlock,
            "AbortDestroyBlock" => PlayerActionType.AbortDestroyBlock,
            "StopDestroyBlock" => PlayerActionType.StopDestroyBlock,
            "StartSleeping" => PlayerActionType.StartSleeping,
            "StopSleeping" => PlayerActionType.StopSleeping,
            "Respawn" => PlayerActionType.Respawn,
            "StartJump" => PlayerActionType.StartJump,
            "StartSprinting" => PlayerActionType.StartSprinting,
            "StopSprinting" => PlayerActionType.StopSprinting,
            "StartSneaking" => PlayerActionType.StartSneaking,
            "StopSneaking" => PlayerActionType.StopSneaking,
            "CreativeDestroyBlock" => PlayerActionType.CreativeDestroyBlock,
            "ChangeDimensionAck" => PlayerActionType.ChangeDimensionAck,
            "StartGliding" => PlayerActionType.StartGliding,
            "StopGliding" => PlayerActionType.StopGliding,
            "DenyDestroyBlock" => PlayerActionType.DenyDestroyBlock,
            "CrackBlock" => PlayerActionType.CrackBlock,
            "StartSwimming" => PlayerActionType.StartSwimming,
            "StopSwimming" => PlayerActionType.StopSwimming,
            "StartSpinAttack" => PlayerActionType.StartSpinAttack,
            "StopSpinAttack" => PlayerActionType.StopSpinAttack,
            "PredictDestroyBlock" => PlayerActionType.PredictDestroyBlock,
            "ContinueDestroyBlock" => PlayerActionType.ContinueDestroyBlock,
            "StartItemUseOn" => PlayerActionType.StartItemUseOn,
            "StopItemUseOn" => PlayerActionType.StopItemUseOn,
            "HandledTeleport" => PlayerActionType.HandledTeleport,
            "MissedSwing" => PlayerActionType.MissedSwing,
            "StartCrawling" => PlayerActionType.StartCrawling,
            "StopCrawling" => PlayerActionType.StopCrawling,
            "StartFlying" => PlayerActionType.StartFlying,
            "StopFlying" => PlayerActionType.StopFlying,
            "StartUsingItem" => PlayerActionType.StartUsingItem,
            "InternalUpdate" => PlayerActionType.InternalUpdate,
            "Count" => PlayerActionType.Count,
            _ => throw new ArgumentException($"Unknown PlayerActionType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PlayerActionType result) {
        switch (value) {
            case "Unknown":
                result = PlayerActionType.Unknown;
                return true;
            case "StartDestroyBlock":
                result = PlayerActionType.StartDestroyBlock;
                return true;
            case "AbortDestroyBlock":
                result = PlayerActionType.AbortDestroyBlock;
                return true;
            case "StopDestroyBlock":
                result = PlayerActionType.StopDestroyBlock;
                return true;
            case "StartSleeping":
                result = PlayerActionType.StartSleeping;
                return true;
            case "StopSleeping":
                result = PlayerActionType.StopSleeping;
                return true;
            case "Respawn":
                result = PlayerActionType.Respawn;
                return true;
            case "StartJump":
                result = PlayerActionType.StartJump;
                return true;
            case "StartSprinting":
                result = PlayerActionType.StartSprinting;
                return true;
            case "StopSprinting":
                result = PlayerActionType.StopSprinting;
                return true;
            case "StartSneaking":
                result = PlayerActionType.StartSneaking;
                return true;
            case "StopSneaking":
                result = PlayerActionType.StopSneaking;
                return true;
            case "CreativeDestroyBlock":
                result = PlayerActionType.CreativeDestroyBlock;
                return true;
            case "ChangeDimensionAck":
                result = PlayerActionType.ChangeDimensionAck;
                return true;
            case "StartGliding":
                result = PlayerActionType.StartGliding;
                return true;
            case "StopGliding":
                result = PlayerActionType.StopGliding;
                return true;
            case "DenyDestroyBlock":
                result = PlayerActionType.DenyDestroyBlock;
                return true;
            case "CrackBlock":
                result = PlayerActionType.CrackBlock;
                return true;
            case "StartSwimming":
                result = PlayerActionType.StartSwimming;
                return true;
            case "StopSwimming":
                result = PlayerActionType.StopSwimming;
                return true;
            case "StartSpinAttack":
                result = PlayerActionType.StartSpinAttack;
                return true;
            case "StopSpinAttack":
                result = PlayerActionType.StopSpinAttack;
                return true;
            case "PredictDestroyBlock":
                result = PlayerActionType.PredictDestroyBlock;
                return true;
            case "ContinueDestroyBlock":
                result = PlayerActionType.ContinueDestroyBlock;
                return true;
            case "StartItemUseOn":
                result = PlayerActionType.StartItemUseOn;
                return true;
            case "StopItemUseOn":
                result = PlayerActionType.StopItemUseOn;
                return true;
            case "HandledTeleport":
                result = PlayerActionType.HandledTeleport;
                return true;
            case "MissedSwing":
                result = PlayerActionType.MissedSwing;
                return true;
            case "StartCrawling":
                result = PlayerActionType.StartCrawling;
                return true;
            case "StopCrawling":
                result = PlayerActionType.StopCrawling;
                return true;
            case "StartFlying":
                result = PlayerActionType.StartFlying;
                return true;
            case "StopFlying":
                result = PlayerActionType.StopFlying;
                return true;
            case "StartUsingItem":
                result = PlayerActionType.StartUsingItem;
                return true;
            case "InternalUpdate":
                result = PlayerActionType.InternalUpdate;
                return true;
            case "Count":
                result = PlayerActionType.Count;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

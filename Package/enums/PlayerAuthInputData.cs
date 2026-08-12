#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum PlayerAuthInputData {
    Ascend = 0,
    Descend = 1,
    JumpDown = 3,
    SprintDown = 4,
    ChangeHeight = 5,
    Jumping = 6,
    AutoJumpingInWater = 7,
    Sneaking = 8,
    SneakDown = 9,
    Up = 10,
    Down = 11,
    Left = 12,
    Right = 13,
    UpLeft = 14,
    UpRight = 15,
    WantUp = 16,
    WantDown = 17,
    WantDownSlow = 18,
    WantUpSlow = 19,
    Sprinting = 20,
    AscendBlock = 21,
    DescendBlock = 22,
    SneakToggleDown = 23,
    PersistSneak = 24,
    StartSprinting = 25,
    StopSprinting = 26,
    StartSneaking = 27,
    StopSneaking = 28,
    StartSwimming = 29,
    StopSwimming = 30,
    StartJumping = 31,
    StartGliding = 32,
    StopGliding = 33,
    PerformItemInteraction = 34,
    PerformBlockActions = 35,
    PerformItemStackRequest = 36,
    HandledTeleport = 37,
    Emoting = 38,
    MissedSwing = 39,
    StartCrawling = 40,
    StopCrawling = 41,
    StartFlying = 42,
    StopFlying = 43,
    ClientAckServerData = 44,
    IsInClientPredictedVehicle = 45,
    PaddlingLeft = 46,
    PaddlingRight = 47,
    BlockBreakingDelayEnabled = 48,
    HorizontalCollision = 49,
    VerticalCollision = 50,
    DownLeft = 51,
    DownRight = 52,
    StartUsingItem = 53,
    StartSpinAttack = 56,
    StopSpinAttack = 57,
    IsHotbarOnlyTouch = 58,
    JumpReleasedRaw = 59,
    JumpPressedRaw = 60,
    JumpCurrentRaw = 61,
    SneakReleasedRaw = 62,
    SneakPressedRaw = 63,
    SneakCurrentRaw = 64,
    InternalUpdate = 65,
}

public static class PlayerAuthInputDataExtensions {
    public static string ToProtoString(this PlayerAuthInputData value) => value.ToProtocolString();

    public static string ToProtocolString(this PlayerAuthInputData value) {
        return value switch {
            PlayerAuthInputData.Ascend => "Ascend",
            PlayerAuthInputData.Descend => "Descend",
            PlayerAuthInputData.JumpDown => "JumpDown",
            PlayerAuthInputData.SprintDown => "SprintDown",
            PlayerAuthInputData.ChangeHeight => "ChangeHeight",
            PlayerAuthInputData.Jumping => "Jumping",
            PlayerAuthInputData.AutoJumpingInWater => "AutoJumpingInWater",
            PlayerAuthInputData.Sneaking => "Sneaking",
            PlayerAuthInputData.SneakDown => "SneakDown",
            PlayerAuthInputData.Up => "Up",
            PlayerAuthInputData.Down => "Down",
            PlayerAuthInputData.Left => "Left",
            PlayerAuthInputData.Right => "Right",
            PlayerAuthInputData.UpLeft => "UpLeft",
            PlayerAuthInputData.UpRight => "UpRight",
            PlayerAuthInputData.WantUp => "WantUp",
            PlayerAuthInputData.WantDown => "WantDown",
            PlayerAuthInputData.WantDownSlow => "WantDownSlow",
            PlayerAuthInputData.WantUpSlow => "WantUpSlow",
            PlayerAuthInputData.Sprinting => "Sprinting",
            PlayerAuthInputData.AscendBlock => "AscendBlock",
            PlayerAuthInputData.DescendBlock => "DescendBlock",
            PlayerAuthInputData.SneakToggleDown => "SneakToggleDown",
            PlayerAuthInputData.PersistSneak => "PersistSneak",
            PlayerAuthInputData.StartSprinting => "StartSprinting",
            PlayerAuthInputData.StopSprinting => "StopSprinting",
            PlayerAuthInputData.StartSneaking => "StartSneaking",
            PlayerAuthInputData.StopSneaking => "StopSneaking",
            PlayerAuthInputData.StartSwimming => "StartSwimming",
            PlayerAuthInputData.StopSwimming => "StopSwimming",
            PlayerAuthInputData.StartJumping => "StartJumping",
            PlayerAuthInputData.StartGliding => "StartGliding",
            PlayerAuthInputData.StopGliding => "StopGliding",
            PlayerAuthInputData.PerformItemInteraction => "PerformItemInteraction",
            PlayerAuthInputData.PerformBlockActions => "PerformBlockActions",
            PlayerAuthInputData.PerformItemStackRequest => "PerformItemStackRequest",
            PlayerAuthInputData.HandledTeleport => "HandledTeleport",
            PlayerAuthInputData.Emoting => "Emoting",
            PlayerAuthInputData.MissedSwing => "MissedSwing",
            PlayerAuthInputData.StartCrawling => "StartCrawling",
            PlayerAuthInputData.StopCrawling => "StopCrawling",
            PlayerAuthInputData.StartFlying => "StartFlying",
            PlayerAuthInputData.StopFlying => "StopFlying",
            PlayerAuthInputData.ClientAckServerData => "ClientAckServerData",
            PlayerAuthInputData.IsInClientPredictedVehicle => "IsInClientPredictedVehicle",
            PlayerAuthInputData.PaddlingLeft => "PaddlingLeft",
            PlayerAuthInputData.PaddlingRight => "PaddlingRight",
            PlayerAuthInputData.BlockBreakingDelayEnabled => "BlockBreakingDelayEnabled",
            PlayerAuthInputData.HorizontalCollision => "HorizontalCollision",
            PlayerAuthInputData.VerticalCollision => "VerticalCollision",
            PlayerAuthInputData.DownLeft => "DownLeft",
            PlayerAuthInputData.DownRight => "DownRight",
            PlayerAuthInputData.StartUsingItem => "StartUsingItem",
            PlayerAuthInputData.StartSpinAttack => "StartSpinAttack",
            PlayerAuthInputData.StopSpinAttack => "StopSpinAttack",
            PlayerAuthInputData.IsHotbarOnlyTouch => "IsHotbarOnlyTouch",
            PlayerAuthInputData.JumpReleasedRaw => "JumpReleasedRaw",
            PlayerAuthInputData.JumpPressedRaw => "JumpPressedRaw",
            PlayerAuthInputData.JumpCurrentRaw => "JumpCurrentRaw",
            PlayerAuthInputData.SneakReleasedRaw => "SneakReleasedRaw",
            PlayerAuthInputData.SneakPressedRaw => "SneakPressedRaw",
            PlayerAuthInputData.SneakCurrentRaw => "SneakCurrentRaw",
            PlayerAuthInputData.InternalUpdate => "InternalUpdate",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PlayerAuthInputData value.")
        };
    }

    public static PlayerAuthInputData FromProtocolString(string value) {
        return value switch {
            "Ascend" => PlayerAuthInputData.Ascend,
            "Descend" => PlayerAuthInputData.Descend,
            "JumpDown" => PlayerAuthInputData.JumpDown,
            "SprintDown" => PlayerAuthInputData.SprintDown,
            "ChangeHeight" => PlayerAuthInputData.ChangeHeight,
            "Jumping" => PlayerAuthInputData.Jumping,
            "AutoJumpingInWater" => PlayerAuthInputData.AutoJumpingInWater,
            "Sneaking" => PlayerAuthInputData.Sneaking,
            "SneakDown" => PlayerAuthInputData.SneakDown,
            "Up" => PlayerAuthInputData.Up,
            "Down" => PlayerAuthInputData.Down,
            "Left" => PlayerAuthInputData.Left,
            "Right" => PlayerAuthInputData.Right,
            "UpLeft" => PlayerAuthInputData.UpLeft,
            "UpRight" => PlayerAuthInputData.UpRight,
            "WantUp" => PlayerAuthInputData.WantUp,
            "WantDown" => PlayerAuthInputData.WantDown,
            "WantDownSlow" => PlayerAuthInputData.WantDownSlow,
            "WantUpSlow" => PlayerAuthInputData.WantUpSlow,
            "Sprinting" => PlayerAuthInputData.Sprinting,
            "AscendBlock" => PlayerAuthInputData.AscendBlock,
            "DescendBlock" => PlayerAuthInputData.DescendBlock,
            "SneakToggleDown" => PlayerAuthInputData.SneakToggleDown,
            "PersistSneak" => PlayerAuthInputData.PersistSneak,
            "StartSprinting" => PlayerAuthInputData.StartSprinting,
            "StopSprinting" => PlayerAuthInputData.StopSprinting,
            "StartSneaking" => PlayerAuthInputData.StartSneaking,
            "StopSneaking" => PlayerAuthInputData.StopSneaking,
            "StartSwimming" => PlayerAuthInputData.StartSwimming,
            "StopSwimming" => PlayerAuthInputData.StopSwimming,
            "StartJumping" => PlayerAuthInputData.StartJumping,
            "StartGliding" => PlayerAuthInputData.StartGliding,
            "StopGliding" => PlayerAuthInputData.StopGliding,
            "PerformItemInteraction" => PlayerAuthInputData.PerformItemInteraction,
            "PerformBlockActions" => PlayerAuthInputData.PerformBlockActions,
            "PerformItemStackRequest" => PlayerAuthInputData.PerformItemStackRequest,
            "HandledTeleport" => PlayerAuthInputData.HandledTeleport,
            "Emoting" => PlayerAuthInputData.Emoting,
            "MissedSwing" => PlayerAuthInputData.MissedSwing,
            "StartCrawling" => PlayerAuthInputData.StartCrawling,
            "StopCrawling" => PlayerAuthInputData.StopCrawling,
            "StartFlying" => PlayerAuthInputData.StartFlying,
            "StopFlying" => PlayerAuthInputData.StopFlying,
            "ClientAckServerData" => PlayerAuthInputData.ClientAckServerData,
            "IsInClientPredictedVehicle" => PlayerAuthInputData.IsInClientPredictedVehicle,
            "PaddlingLeft" => PlayerAuthInputData.PaddlingLeft,
            "PaddlingRight" => PlayerAuthInputData.PaddlingRight,
            "BlockBreakingDelayEnabled" => PlayerAuthInputData.BlockBreakingDelayEnabled,
            "HorizontalCollision" => PlayerAuthInputData.HorizontalCollision,
            "VerticalCollision" => PlayerAuthInputData.VerticalCollision,
            "DownLeft" => PlayerAuthInputData.DownLeft,
            "DownRight" => PlayerAuthInputData.DownRight,
            "StartUsingItem" => PlayerAuthInputData.StartUsingItem,
            "StartSpinAttack" => PlayerAuthInputData.StartSpinAttack,
            "StopSpinAttack" => PlayerAuthInputData.StopSpinAttack,
            "IsHotbarOnlyTouch" => PlayerAuthInputData.IsHotbarOnlyTouch,
            "JumpReleasedRaw" => PlayerAuthInputData.JumpReleasedRaw,
            "JumpPressedRaw" => PlayerAuthInputData.JumpPressedRaw,
            "JumpCurrentRaw" => PlayerAuthInputData.JumpCurrentRaw,
            "SneakReleasedRaw" => PlayerAuthInputData.SneakReleasedRaw,
            "SneakPressedRaw" => PlayerAuthInputData.SneakPressedRaw,
            "SneakCurrentRaw" => PlayerAuthInputData.SneakCurrentRaw,
            "InternalUpdate" => PlayerAuthInputData.InternalUpdate,
            _ => throw new ArgumentException($"Unknown PlayerAuthInputData protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PlayerAuthInputData result) {
        switch (value) {
            case "Ascend":
                result = PlayerAuthInputData.Ascend;
                return true;
            case "Descend":
                result = PlayerAuthInputData.Descend;
                return true;
            case "JumpDown":
                result = PlayerAuthInputData.JumpDown;
                return true;
            case "SprintDown":
                result = PlayerAuthInputData.SprintDown;
                return true;
            case "ChangeHeight":
                result = PlayerAuthInputData.ChangeHeight;
                return true;
            case "Jumping":
                result = PlayerAuthInputData.Jumping;
                return true;
            case "AutoJumpingInWater":
                result = PlayerAuthInputData.AutoJumpingInWater;
                return true;
            case "Sneaking":
                result = PlayerAuthInputData.Sneaking;
                return true;
            case "SneakDown":
                result = PlayerAuthInputData.SneakDown;
                return true;
            case "Up":
                result = PlayerAuthInputData.Up;
                return true;
            case "Down":
                result = PlayerAuthInputData.Down;
                return true;
            case "Left":
                result = PlayerAuthInputData.Left;
                return true;
            case "Right":
                result = PlayerAuthInputData.Right;
                return true;
            case "UpLeft":
                result = PlayerAuthInputData.UpLeft;
                return true;
            case "UpRight":
                result = PlayerAuthInputData.UpRight;
                return true;
            case "WantUp":
                result = PlayerAuthInputData.WantUp;
                return true;
            case "WantDown":
                result = PlayerAuthInputData.WantDown;
                return true;
            case "WantDownSlow":
                result = PlayerAuthInputData.WantDownSlow;
                return true;
            case "WantUpSlow":
                result = PlayerAuthInputData.WantUpSlow;
                return true;
            case "Sprinting":
                result = PlayerAuthInputData.Sprinting;
                return true;
            case "AscendBlock":
                result = PlayerAuthInputData.AscendBlock;
                return true;
            case "DescendBlock":
                result = PlayerAuthInputData.DescendBlock;
                return true;
            case "SneakToggleDown":
                result = PlayerAuthInputData.SneakToggleDown;
                return true;
            case "PersistSneak":
                result = PlayerAuthInputData.PersistSneak;
                return true;
            case "StartSprinting":
                result = PlayerAuthInputData.StartSprinting;
                return true;
            case "StopSprinting":
                result = PlayerAuthInputData.StopSprinting;
                return true;
            case "StartSneaking":
                result = PlayerAuthInputData.StartSneaking;
                return true;
            case "StopSneaking":
                result = PlayerAuthInputData.StopSneaking;
                return true;
            case "StartSwimming":
                result = PlayerAuthInputData.StartSwimming;
                return true;
            case "StopSwimming":
                result = PlayerAuthInputData.StopSwimming;
                return true;
            case "StartJumping":
                result = PlayerAuthInputData.StartJumping;
                return true;
            case "StartGliding":
                result = PlayerAuthInputData.StartGliding;
                return true;
            case "StopGliding":
                result = PlayerAuthInputData.StopGliding;
                return true;
            case "PerformItemInteraction":
                result = PlayerAuthInputData.PerformItemInteraction;
                return true;
            case "PerformBlockActions":
                result = PlayerAuthInputData.PerformBlockActions;
                return true;
            case "PerformItemStackRequest":
                result = PlayerAuthInputData.PerformItemStackRequest;
                return true;
            case "HandledTeleport":
                result = PlayerAuthInputData.HandledTeleport;
                return true;
            case "Emoting":
                result = PlayerAuthInputData.Emoting;
                return true;
            case "MissedSwing":
                result = PlayerAuthInputData.MissedSwing;
                return true;
            case "StartCrawling":
                result = PlayerAuthInputData.StartCrawling;
                return true;
            case "StopCrawling":
                result = PlayerAuthInputData.StopCrawling;
                return true;
            case "StartFlying":
                result = PlayerAuthInputData.StartFlying;
                return true;
            case "StopFlying":
                result = PlayerAuthInputData.StopFlying;
                return true;
            case "ClientAckServerData":
                result = PlayerAuthInputData.ClientAckServerData;
                return true;
            case "IsInClientPredictedVehicle":
                result = PlayerAuthInputData.IsInClientPredictedVehicle;
                return true;
            case "PaddlingLeft":
                result = PlayerAuthInputData.PaddlingLeft;
                return true;
            case "PaddlingRight":
                result = PlayerAuthInputData.PaddlingRight;
                return true;
            case "BlockBreakingDelayEnabled":
                result = PlayerAuthInputData.BlockBreakingDelayEnabled;
                return true;
            case "HorizontalCollision":
                result = PlayerAuthInputData.HorizontalCollision;
                return true;
            case "VerticalCollision":
                result = PlayerAuthInputData.VerticalCollision;
                return true;
            case "DownLeft":
                result = PlayerAuthInputData.DownLeft;
                return true;
            case "DownRight":
                result = PlayerAuthInputData.DownRight;
                return true;
            case "StartUsingItem":
                result = PlayerAuthInputData.StartUsingItem;
                return true;
            case "StartSpinAttack":
                result = PlayerAuthInputData.StartSpinAttack;
                return true;
            case "StopSpinAttack":
                result = PlayerAuthInputData.StopSpinAttack;
                return true;
            case "IsHotbarOnlyTouch":
                result = PlayerAuthInputData.IsHotbarOnlyTouch;
                return true;
            case "JumpReleasedRaw":
                result = PlayerAuthInputData.JumpReleasedRaw;
                return true;
            case "JumpPressedRaw":
                result = PlayerAuthInputData.JumpPressedRaw;
                return true;
            case "JumpCurrentRaw":
                result = PlayerAuthInputData.JumpCurrentRaw;
                return true;
            case "SneakReleasedRaw":
                result = PlayerAuthInputData.SneakReleasedRaw;
                return true;
            case "SneakPressedRaw":
                result = PlayerAuthInputData.SneakPressedRaw;
                return true;
            case "SneakCurrentRaw":
                result = PlayerAuthInputData.SneakCurrentRaw;
                return true;
            case "InternalUpdate":
                result = PlayerAuthInputData.InternalUpdate;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

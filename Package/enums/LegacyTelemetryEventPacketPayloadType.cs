using System;

namespace BedrockProtocol.Enums;

public enum LegacyTelemetryEventPacketPayloadType {
    Achievement = 0,
    Interaction = 1,
    PortalCreated = 2,
    PortalUsed = 3,
    MobKilled = 4,
    CauldronUsed = 5,
    PlayerDied = 6,
    BossKilled = 7,
    AgentCommand_OBSOLETE = 8,
    AgentCreated = 9,
    PatternRemoved_OBSOLETE = 10,
    SlashCommand = 11,
    FishBucketed_OBSOLETE = 12,
    MobBorn = 13,
    PetDied_OBSOLETE = 14,
    POICauldronUsed = 15,
    ComposterUsed = 16,
    BellUsed = 17,
    ActorDefinition = 18,
    RaidUpdate = 19,
    PlayerMovementAnomaly_OBSOLETE = 20,
    PlayerMovementCorrected_OBSOLETE = 21,
    HoneyHarvested = 22,
    TargetBlockHit = 23,
    PiglinBarter = 24,
    PlayerWaxedOrUnwaxedCopper = 25,
    CodeBuilderRuntimeAction = 26,
    CodeBuilderScoreboard = 27,
    StriderRiddenInLavaInOverworld = 28,
    SneakCloseToSculkSensor = 29,
    CarefulRestoration = 30,
    ItemUsed = 31,
}

public static class LegacyTelemetryEventPacketPayloadTypeExtensions {
    public static string ToProtoString(this LegacyTelemetryEventPacketPayloadType value) => value.ToProtocolString();

    public static string ToProtocolString(this LegacyTelemetryEventPacketPayloadType value) {
        return value switch {
            LegacyTelemetryEventPacketPayloadType.Achievement => "Achievement",
            LegacyTelemetryEventPacketPayloadType.Interaction => "Interaction",
            LegacyTelemetryEventPacketPayloadType.PortalCreated => "PortalCreated",
            LegacyTelemetryEventPacketPayloadType.PortalUsed => "PortalUsed",
            LegacyTelemetryEventPacketPayloadType.MobKilled => "MobKilled",
            LegacyTelemetryEventPacketPayloadType.CauldronUsed => "CauldronUsed",
            LegacyTelemetryEventPacketPayloadType.PlayerDied => "PlayerDied",
            LegacyTelemetryEventPacketPayloadType.BossKilled => "BossKilled",
            LegacyTelemetryEventPacketPayloadType.AgentCommand_OBSOLETE => "AgentCommand_OBSOLETE",
            LegacyTelemetryEventPacketPayloadType.AgentCreated => "AgentCreated",
            LegacyTelemetryEventPacketPayloadType.PatternRemoved_OBSOLETE => "PatternRemoved_OBSOLETE",
            LegacyTelemetryEventPacketPayloadType.SlashCommand => "SlashCommand",
            LegacyTelemetryEventPacketPayloadType.FishBucketed_OBSOLETE => "FishBucketed_OBSOLETE",
            LegacyTelemetryEventPacketPayloadType.MobBorn => "MobBorn",
            LegacyTelemetryEventPacketPayloadType.PetDied_OBSOLETE => "PetDied_OBSOLETE",
            LegacyTelemetryEventPacketPayloadType.POICauldronUsed => "POICauldronUsed",
            LegacyTelemetryEventPacketPayloadType.ComposterUsed => "ComposterUsed",
            LegacyTelemetryEventPacketPayloadType.BellUsed => "BellUsed",
            LegacyTelemetryEventPacketPayloadType.ActorDefinition => "ActorDefinition",
            LegacyTelemetryEventPacketPayloadType.RaidUpdate => "RaidUpdate",
            LegacyTelemetryEventPacketPayloadType.PlayerMovementAnomaly_OBSOLETE => "PlayerMovementAnomaly_OBSOLETE",
            LegacyTelemetryEventPacketPayloadType.PlayerMovementCorrected_OBSOLETE => "PlayerMovementCorrected_OBSOLETE",
            LegacyTelemetryEventPacketPayloadType.HoneyHarvested => "HoneyHarvested",
            LegacyTelemetryEventPacketPayloadType.TargetBlockHit => "TargetBlockHit",
            LegacyTelemetryEventPacketPayloadType.PiglinBarter => "PiglinBarter",
            LegacyTelemetryEventPacketPayloadType.PlayerWaxedOrUnwaxedCopper => "PlayerWaxedOrUnwaxedCopper",
            LegacyTelemetryEventPacketPayloadType.CodeBuilderRuntimeAction => "CodeBuilderRuntimeAction",
            LegacyTelemetryEventPacketPayloadType.CodeBuilderScoreboard => "CodeBuilderScoreboard",
            LegacyTelemetryEventPacketPayloadType.StriderRiddenInLavaInOverworld => "StriderRiddenInLavaInOverworld",
            LegacyTelemetryEventPacketPayloadType.SneakCloseToSculkSensor => "SneakCloseToSculkSensor",
            LegacyTelemetryEventPacketPayloadType.CarefulRestoration => "CarefulRestoration",
            LegacyTelemetryEventPacketPayloadType.ItemUsed => "ItemUsed",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown LegacyTelemetryEventPacketPayloadType value.")
        };
    }

    public static LegacyTelemetryEventPacketPayloadType FromProtocolString(string value) {
        return value switch {
            "Achievement" => LegacyTelemetryEventPacketPayloadType.Achievement,
            "Interaction" => LegacyTelemetryEventPacketPayloadType.Interaction,
            "PortalCreated" => LegacyTelemetryEventPacketPayloadType.PortalCreated,
            "PortalUsed" => LegacyTelemetryEventPacketPayloadType.PortalUsed,
            "MobKilled" => LegacyTelemetryEventPacketPayloadType.MobKilled,
            "CauldronUsed" => LegacyTelemetryEventPacketPayloadType.CauldronUsed,
            "PlayerDied" => LegacyTelemetryEventPacketPayloadType.PlayerDied,
            "BossKilled" => LegacyTelemetryEventPacketPayloadType.BossKilled,
            "AgentCommand_OBSOLETE" => LegacyTelemetryEventPacketPayloadType.AgentCommand_OBSOLETE,
            "AgentCreated" => LegacyTelemetryEventPacketPayloadType.AgentCreated,
            "PatternRemoved_OBSOLETE" => LegacyTelemetryEventPacketPayloadType.PatternRemoved_OBSOLETE,
            "SlashCommand" => LegacyTelemetryEventPacketPayloadType.SlashCommand,
            "FishBucketed_OBSOLETE" => LegacyTelemetryEventPacketPayloadType.FishBucketed_OBSOLETE,
            "MobBorn" => LegacyTelemetryEventPacketPayloadType.MobBorn,
            "PetDied_OBSOLETE" => LegacyTelemetryEventPacketPayloadType.PetDied_OBSOLETE,
            "POICauldronUsed" => LegacyTelemetryEventPacketPayloadType.POICauldronUsed,
            "ComposterUsed" => LegacyTelemetryEventPacketPayloadType.ComposterUsed,
            "BellUsed" => LegacyTelemetryEventPacketPayloadType.BellUsed,
            "ActorDefinition" => LegacyTelemetryEventPacketPayloadType.ActorDefinition,
            "RaidUpdate" => LegacyTelemetryEventPacketPayloadType.RaidUpdate,
            "PlayerMovementAnomaly_OBSOLETE" => LegacyTelemetryEventPacketPayloadType.PlayerMovementAnomaly_OBSOLETE,
            "PlayerMovementCorrected_OBSOLETE" => LegacyTelemetryEventPacketPayloadType.PlayerMovementCorrected_OBSOLETE,
            "HoneyHarvested" => LegacyTelemetryEventPacketPayloadType.HoneyHarvested,
            "TargetBlockHit" => LegacyTelemetryEventPacketPayloadType.TargetBlockHit,
            "PiglinBarter" => LegacyTelemetryEventPacketPayloadType.PiglinBarter,
            "PlayerWaxedOrUnwaxedCopper" => LegacyTelemetryEventPacketPayloadType.PlayerWaxedOrUnwaxedCopper,
            "CodeBuilderRuntimeAction" => LegacyTelemetryEventPacketPayloadType.CodeBuilderRuntimeAction,
            "CodeBuilderScoreboard" => LegacyTelemetryEventPacketPayloadType.CodeBuilderScoreboard,
            "StriderRiddenInLavaInOverworld" => LegacyTelemetryEventPacketPayloadType.StriderRiddenInLavaInOverworld,
            "SneakCloseToSculkSensor" => LegacyTelemetryEventPacketPayloadType.SneakCloseToSculkSensor,
            "CarefulRestoration" => LegacyTelemetryEventPacketPayloadType.CarefulRestoration,
            "ItemUsed" => LegacyTelemetryEventPacketPayloadType.ItemUsed,
            _ => throw new ArgumentException($"Unknown LegacyTelemetryEventPacketPayloadType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out LegacyTelemetryEventPacketPayloadType result) {
        switch (value) {
            case "Achievement":
                result = LegacyTelemetryEventPacketPayloadType.Achievement;
                return true;
            case "Interaction":
                result = LegacyTelemetryEventPacketPayloadType.Interaction;
                return true;
            case "PortalCreated":
                result = LegacyTelemetryEventPacketPayloadType.PortalCreated;
                return true;
            case "PortalUsed":
                result = LegacyTelemetryEventPacketPayloadType.PortalUsed;
                return true;
            case "MobKilled":
                result = LegacyTelemetryEventPacketPayloadType.MobKilled;
                return true;
            case "CauldronUsed":
                result = LegacyTelemetryEventPacketPayloadType.CauldronUsed;
                return true;
            case "PlayerDied":
                result = LegacyTelemetryEventPacketPayloadType.PlayerDied;
                return true;
            case "BossKilled":
                result = LegacyTelemetryEventPacketPayloadType.BossKilled;
                return true;
            case "AgentCommand_OBSOLETE":
                result = LegacyTelemetryEventPacketPayloadType.AgentCommand_OBSOLETE;
                return true;
            case "AgentCreated":
                result = LegacyTelemetryEventPacketPayloadType.AgentCreated;
                return true;
            case "PatternRemoved_OBSOLETE":
                result = LegacyTelemetryEventPacketPayloadType.PatternRemoved_OBSOLETE;
                return true;
            case "SlashCommand":
                result = LegacyTelemetryEventPacketPayloadType.SlashCommand;
                return true;
            case "FishBucketed_OBSOLETE":
                result = LegacyTelemetryEventPacketPayloadType.FishBucketed_OBSOLETE;
                return true;
            case "MobBorn":
                result = LegacyTelemetryEventPacketPayloadType.MobBorn;
                return true;
            case "PetDied_OBSOLETE":
                result = LegacyTelemetryEventPacketPayloadType.PetDied_OBSOLETE;
                return true;
            case "POICauldronUsed":
                result = LegacyTelemetryEventPacketPayloadType.POICauldronUsed;
                return true;
            case "ComposterUsed":
                result = LegacyTelemetryEventPacketPayloadType.ComposterUsed;
                return true;
            case "BellUsed":
                result = LegacyTelemetryEventPacketPayloadType.BellUsed;
                return true;
            case "ActorDefinition":
                result = LegacyTelemetryEventPacketPayloadType.ActorDefinition;
                return true;
            case "RaidUpdate":
                result = LegacyTelemetryEventPacketPayloadType.RaidUpdate;
                return true;
            case "PlayerMovementAnomaly_OBSOLETE":
                result = LegacyTelemetryEventPacketPayloadType.PlayerMovementAnomaly_OBSOLETE;
                return true;
            case "PlayerMovementCorrected_OBSOLETE":
                result = LegacyTelemetryEventPacketPayloadType.PlayerMovementCorrected_OBSOLETE;
                return true;
            case "HoneyHarvested":
                result = LegacyTelemetryEventPacketPayloadType.HoneyHarvested;
                return true;
            case "TargetBlockHit":
                result = LegacyTelemetryEventPacketPayloadType.TargetBlockHit;
                return true;
            case "PiglinBarter":
                result = LegacyTelemetryEventPacketPayloadType.PiglinBarter;
                return true;
            case "PlayerWaxedOrUnwaxedCopper":
                result = LegacyTelemetryEventPacketPayloadType.PlayerWaxedOrUnwaxedCopper;
                return true;
            case "CodeBuilderRuntimeAction":
                result = LegacyTelemetryEventPacketPayloadType.CodeBuilderRuntimeAction;
                return true;
            case "CodeBuilderScoreboard":
                result = LegacyTelemetryEventPacketPayloadType.CodeBuilderScoreboard;
                return true;
            case "StriderRiddenInLavaInOverworld":
                result = LegacyTelemetryEventPacketPayloadType.StriderRiddenInLavaInOverworld;
                return true;
            case "SneakCloseToSculkSensor":
                result = LegacyTelemetryEventPacketPayloadType.SneakCloseToSculkSensor;
                return true;
            case "CarefulRestoration":
                result = LegacyTelemetryEventPacketPayloadType.CarefulRestoration;
                return true;
            case "ItemUsed":
                result = LegacyTelemetryEventPacketPayloadType.ItemUsed;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum MinecraftPacketIds {
    KeepAlive = 0,
    Login = 1,
    PlayStatus = 2,
    ServerToClientHandshake = 3,
    ClientToServerHandshake = 4,
    Disconnect = 5,
    ResourcePacksInfo = 6,
    ResourcePackStack = 7,
    ResourcePackClientResponse = 8,
    Text = 9,
    SetTime = 10,
    StartGame = 11,
    AddPlayer = 12,
    AddActor = 13,
    RemoveActor = 14,
    AddItemActor = 15,
    ServerPlayerPostMovePosition = 16,
    TakeItemActor = 17,
    MoveAbsoluteActor = 18,
    MovePlayer = 19,
    UpdateBlock = 21,
    AddPainting = 22,
    LevelEvent = 25,
    TileEvent = 26,
    ActorEvent = 27,
    MobEffect = 28,
    UpdateAttributes = 29,
    InventoryTransaction = 30,
    PlayerEquipment = 31,
    MobArmorEquipment = 32,
    Interact = 33,
    BlockPickRequest = 34,
    ActorPickRequest = 35,
    PlayerAction = 36,
    HurtArmor = 38,
    SetActorData = 39,
    SetActorMotion = 40,
    SetActorLink = 41,
    SetHealth = 42,
    SetSpawnPosition = 43,
    Animate = 44,
    Respawn = 45,
    ContainerOpen = 46,
    ContainerClose = 47,
    PlayerHotbar = 48,
    InventoryContent = 49,
    InventorySlot = 50,
    ContainerSetData = 51,
    CraftingData = 52,
    GuiDataPickItem = 54,
    BlockActorData = 56,
    FullChunkData = 58,
    SetCommandsEnabled = 59,
    SetDifficulty = 60,
    ChangeDimension = 61,
    SetPlayerGameType = 62,
    PlayerList = 63,
    SimpleEvent = 64,
    LegacyTelemetryEvent = 65,
    SpawnExperienceOrb = 66,
    MapData = 67,
    MapInfoRequest = 68,
    RequestChunkRadius = 69,
    ChunkRadiusUpdated = 70,
    GameRulesChanged = 72,
    Camera = 73,
    BossEvent = 74,
    ShowCredits = 75,
    AvailableCommands = 76,
    CommandRequest = 77,
    CommandBlockUpdate = 78,
    CommandOutput = 79,
    UpdateTrade = 80,
    UpdateEquip = 81,
    ResourcePackDataInfo = 82,
    ResourcePackChunkData = 83,
    ResourcePackChunkRequest = 84,
    Transfer = 85,
    PlaySound = 86,
    StopSound = 87,
    SetTitle = 88,
    AddBehaviorTree = 89,
    StructureBlockUpdate = 90,
    ShowStoreOffer = 91,
    PurchaseReceipt = 92,
    PlayerSkin = 93,
    SubclientLogin = 94,
    AutomationClientConnect = 95,
    SetLastHurtBy = 96,
    BookEdit = 97,
    NPCRequest = 98,
    PhotoTransfer = 99,
    ShowModalForm = 100,
    ModalFormResponse = 101,
    ServerSettingsRequest = 102,
    ServerSettingsResponse = 103,
    ShowProfile = 104,
    SetDefaultGameType = 105,
    RemoveObjective = 106,
    SetDisplayObjective = 107,
    SetScore = 108,
    LabTable = 109,
    UpdateBlockSynced = 110,
    MoveDeltaActor = 111,
    SetScoreboardIdentity = 112,
    SetLocalPlayerAsInit = 113,
    UpdateSoftEnum = 114,
    Ping = 115,
    ScriptCustomEvent = 117,
    SpawnParticleEffect = 118,
    AvailableActorIDList = 119,
    NetworkChunkPublisherUpdate = 121,
    BiomeDefinitionList = 122,
    LevelSoundEvent = 123,
    LevelEventGeneric = 124,
    LecternUpdate = 125,
    ClientCacheStatus = 129,
    OnScreenTextureAnimation = 130,
    MapCreateLockedCopy = 131,
    StructureTemplateDataExportRequest = 132,
    StructureTemplateDataExportResponse = 133,
    ClientCacheBlobStatusPacket = 135,
    ClientCacheMissResponsePacket = 136,
    EducationSettingsPacket = 137,
    Emote = 138,
    MultiplayerSettingsPacket = 139,
    SettingsCommandPacket = 140,
    AnvilDamage = 141,
    CompletedUsingItem = 142,
    NetworkSettings = 143,
    PlayerAuthInputPacket = 144,
    CreativeContent = 145,
    PlayerEnchantOptions = 146,
    ItemStackRequest = 147,
    ItemStackResponse = 148,
    PlayerArmorDamage = 149,
    CodeBuilderPacket = 150,
    UpdatePlayerGameType = 151,
    EmoteList = 152,
    PositionTrackingDBServerBroadcast = 153,
    PositionTrackingDBClientRequest = 154,
    DebugInfoPacket = 155,
    PacketViolationWarning = 156,
    MotionPredictionHints = 157,
    TriggerAnimation = 158,
    CameraShake = 159,
    PlayerFogSetting = 160,
    CorrectPlayerMovePredictionPacket = 161,
    ItemRegistryPacket = 162,
    ClientBoundDebugRendererPacket = 164,
    SyncActorProperty = 165,
    AddVolumeEntityPacket = 166,
    RemoveVolumeEntityPacket = 167,
    SimulationTypePacket = 168,
    NpcDialoguePacket = 169,
    EduUriResourcePacket = 170,
    CreatePhotoPacket = 171,
    UpdateSubChunkBlocks = 172,
    SubChunkPacket = 174,
    SubChunkRequestPacket = 175,
    PlayerStartItemCooldown = 176,
    ScriptMessagePacket = 177,
    CodeBuilderSourcePacket = 178,
    TickingAreasLoadStatus = 179,
    DimensionDataPacket = 180,
    AgentAction = 181,
    ChangeMobProperty = 182,
    LessonProgressPacket = 183,
    RequestAbilityPacket = 184,
    RequestPermissionsPacket = 185,
    ToastRequest = 186,
    UpdateAbilitiesPacket = 187,
    UpdateAdventureSettingsPacket = 188,
    DeathInfo = 189,
    EditorNetworkPacket = 190,
    FeatureRegistryPacket = 191,
    ServerStats = 192,
    RequestNetworkSettings = 193,
    GameTestRequestPacket = 194,
    GameTestResultsPacket = 195,
    PlayerClientInputPermissions = 196,
    CameraPresets = 198,
    UnlockedRecipes = 199,
    TitleSpecificPacketsStart = 200,
    TitleSpecificPacketsEnd = 299,
    CameraInstruction = 300,
    TrimData = 302,
    OpenSign = 303,
    AgentAnimation = 304,
    RefreshEntitlementsPacket = 305,
    PlayerToggleCrafterSlotRequestPacket = 306,
    SetPlayerInventoryOptions = 307,
    SetHudPacket = 308,
    AwardAchievementPacket = 309,
    ClientboundCloseScreen = 310,
    ServerboundLoadingScreenPacket = 312,
    JigsawStructureDataPacket = 313,
    CurrentStructureFeaturePacket = 314,
    ServerboundDiagnosticsPacket = 315,
    CameraAimAssist = 316,
    ContainerRegistryCleanup = 317,
    MovementEffect = 318,
    CameraAimAssistActorPriority = 339,
    CameraAimAssistPresets = 320,
    ClientCameraAimAssist = 321,
    ClientMovementPredictionSyncPacket = 322,
    UpdateClientOptions = 323,
    PlayerVideoCapturePacket = 324,
    PlayerUpdateEntityOverridesPacket = 325,
    PlayerLocation = 326,
    SyncWorldClocks = 344,
    SendPartyDestinationCookie = 349,
    PartyDestinationCookieResponse = 350,
}

public static class MinecraftPacketIdsExtensions {
    public static string ToProtoString(this MinecraftPacketIds value) => value.ToProtocolString();

    public static string ToProtocolString(this MinecraftPacketIds value) {
        return value switch {
            MinecraftPacketIds.KeepAlive => "KeepAlive",
            MinecraftPacketIds.Login => "Login",
            MinecraftPacketIds.PlayStatus => "PlayStatus",
            MinecraftPacketIds.ServerToClientHandshake => "ServerToClientHandshake",
            MinecraftPacketIds.ClientToServerHandshake => "ClientToServerHandshake",
            MinecraftPacketIds.Disconnect => "Disconnect",
            MinecraftPacketIds.ResourcePacksInfo => "ResourcePacksInfo",
            MinecraftPacketIds.ResourcePackStack => "ResourcePackStack",
            MinecraftPacketIds.ResourcePackClientResponse => "ResourcePackClientResponse",
            MinecraftPacketIds.Text => "Text",
            MinecraftPacketIds.SetTime => "SetTime",
            MinecraftPacketIds.StartGame => "StartGame",
            MinecraftPacketIds.AddPlayer => "AddPlayer",
            MinecraftPacketIds.AddActor => "AddActor",
            MinecraftPacketIds.RemoveActor => "RemoveActor",
            MinecraftPacketIds.AddItemActor => "AddItemActor",
            MinecraftPacketIds.ServerPlayerPostMovePosition => "ServerPlayerPostMovePosition",
            MinecraftPacketIds.TakeItemActor => "TakeItemActor",
            MinecraftPacketIds.MoveAbsoluteActor => "MoveAbsoluteActor",
            MinecraftPacketIds.MovePlayer => "MovePlayer",
            MinecraftPacketIds.UpdateBlock => "UpdateBlock",
            MinecraftPacketIds.AddPainting => "AddPainting",
            MinecraftPacketIds.LevelEvent => "LevelEvent",
            MinecraftPacketIds.TileEvent => "TileEvent",
            MinecraftPacketIds.ActorEvent => "ActorEvent",
            MinecraftPacketIds.MobEffect => "MobEffect",
            MinecraftPacketIds.UpdateAttributes => "UpdateAttributes",
            MinecraftPacketIds.InventoryTransaction => "InventoryTransaction",
            MinecraftPacketIds.PlayerEquipment => "PlayerEquipment",
            MinecraftPacketIds.MobArmorEquipment => "MobArmorEquipment",
            MinecraftPacketIds.Interact => "Interact",
            MinecraftPacketIds.BlockPickRequest => "BlockPickRequest",
            MinecraftPacketIds.ActorPickRequest => "ActorPickRequest",
            MinecraftPacketIds.PlayerAction => "PlayerAction",
            MinecraftPacketIds.HurtArmor => "HurtArmor",
            MinecraftPacketIds.SetActorData => "SetActorData",
            MinecraftPacketIds.SetActorMotion => "SetActorMotion",
            MinecraftPacketIds.SetActorLink => "SetActorLink",
            MinecraftPacketIds.SetHealth => "SetHealth",
            MinecraftPacketIds.SetSpawnPosition => "SetSpawnPosition",
            MinecraftPacketIds.Animate => "Animate",
            MinecraftPacketIds.Respawn => "Respawn",
            MinecraftPacketIds.ContainerOpen => "ContainerOpen",
            MinecraftPacketIds.ContainerClose => "ContainerClose",
            MinecraftPacketIds.PlayerHotbar => "PlayerHotbar",
            MinecraftPacketIds.InventoryContent => "InventoryContent",
            MinecraftPacketIds.InventorySlot => "InventorySlot",
            MinecraftPacketIds.ContainerSetData => "ContainerSetData",
            MinecraftPacketIds.CraftingData => "CraftingData",
            MinecraftPacketIds.GuiDataPickItem => "GuiDataPickItem",
            MinecraftPacketIds.BlockActorData => "BlockActorData",
            MinecraftPacketIds.FullChunkData => "FullChunkData",
            MinecraftPacketIds.SetCommandsEnabled => "SetCommandsEnabled",
            MinecraftPacketIds.SetDifficulty => "SetDifficulty",
            MinecraftPacketIds.ChangeDimension => "ChangeDimension",
            MinecraftPacketIds.SetPlayerGameType => "SetPlayerGameType",
            MinecraftPacketIds.PlayerList => "PlayerList",
            MinecraftPacketIds.SimpleEvent => "SimpleEvent",
            MinecraftPacketIds.LegacyTelemetryEvent => "LegacyTelemetryEvent",
            MinecraftPacketIds.SpawnExperienceOrb => "SpawnExperienceOrb",
            MinecraftPacketIds.MapData => "MapData",
            MinecraftPacketIds.MapInfoRequest => "MapInfoRequest",
            MinecraftPacketIds.RequestChunkRadius => "RequestChunkRadius",
            MinecraftPacketIds.ChunkRadiusUpdated => "ChunkRadiusUpdated",
            MinecraftPacketIds.GameRulesChanged => "GameRulesChanged",
            MinecraftPacketIds.Camera => "Camera",
            MinecraftPacketIds.BossEvent => "BossEvent",
            MinecraftPacketIds.ShowCredits => "ShowCredits",
            MinecraftPacketIds.AvailableCommands => "AvailableCommands",
            MinecraftPacketIds.CommandRequest => "CommandRequest",
            MinecraftPacketIds.CommandBlockUpdate => "CommandBlockUpdate",
            MinecraftPacketIds.CommandOutput => "CommandOutput",
            MinecraftPacketIds.UpdateTrade => "UpdateTrade",
            MinecraftPacketIds.UpdateEquip => "UpdateEquip",
            MinecraftPacketIds.ResourcePackDataInfo => "ResourcePackDataInfo",
            MinecraftPacketIds.ResourcePackChunkData => "ResourcePackChunkData",
            MinecraftPacketIds.ResourcePackChunkRequest => "ResourcePackChunkRequest",
            MinecraftPacketIds.Transfer => "Transfer",
            MinecraftPacketIds.PlaySound => "PlaySound",
            MinecraftPacketIds.StopSound => "StopSound",
            MinecraftPacketIds.SetTitle => "SetTitle",
            MinecraftPacketIds.AddBehaviorTree => "AddBehaviorTree",
            MinecraftPacketIds.StructureBlockUpdate => "StructureBlockUpdate",
            MinecraftPacketIds.ShowStoreOffer => "ShowStoreOffer",
            MinecraftPacketIds.PurchaseReceipt => "PurchaseReceipt",
            MinecraftPacketIds.PlayerSkin => "PlayerSkin",
            MinecraftPacketIds.SubclientLogin => "SubclientLogin",
            MinecraftPacketIds.AutomationClientConnect => "AutomationClientConnect",
            MinecraftPacketIds.SetLastHurtBy => "SetLastHurtBy",
            MinecraftPacketIds.BookEdit => "BookEdit",
            MinecraftPacketIds.NPCRequest => "NPCRequest",
            MinecraftPacketIds.PhotoTransfer => "PhotoTransfer",
            MinecraftPacketIds.ShowModalForm => "ShowModalForm",
            MinecraftPacketIds.ModalFormResponse => "ModalFormResponse",
            MinecraftPacketIds.ServerSettingsRequest => "ServerSettingsRequest",
            MinecraftPacketIds.ServerSettingsResponse => "ServerSettingsResponse",
            MinecraftPacketIds.ShowProfile => "ShowProfile",
            MinecraftPacketIds.SetDefaultGameType => "SetDefaultGameType",
            MinecraftPacketIds.RemoveObjective => "RemoveObjective",
            MinecraftPacketIds.SetDisplayObjective => "SetDisplayObjective",
            MinecraftPacketIds.SetScore => "SetScore",
            MinecraftPacketIds.LabTable => "LabTable",
            MinecraftPacketIds.UpdateBlockSynced => "UpdateBlockSynced",
            MinecraftPacketIds.MoveDeltaActor => "MoveDeltaActor",
            MinecraftPacketIds.SetScoreboardIdentity => "SetScoreboardIdentity",
            MinecraftPacketIds.SetLocalPlayerAsInit => "SetLocalPlayerAsInit",
            MinecraftPacketIds.UpdateSoftEnum => "UpdateSoftEnum",
            MinecraftPacketIds.Ping => "Ping",
            MinecraftPacketIds.ScriptCustomEvent => "ScriptCustomEvent",
            MinecraftPacketIds.SpawnParticleEffect => "SpawnParticleEffect",
            MinecraftPacketIds.AvailableActorIDList => "AvailableActorIDList",
            MinecraftPacketIds.NetworkChunkPublisherUpdate => "NetworkChunkPublisherUpdate",
            MinecraftPacketIds.BiomeDefinitionList => "BiomeDefinitionList",
            MinecraftPacketIds.LevelSoundEvent => "LevelSoundEvent",
            MinecraftPacketIds.LevelEventGeneric => "LevelEventGeneric",
            MinecraftPacketIds.LecternUpdate => "LecternUpdate",
            MinecraftPacketIds.ClientCacheStatus => "ClientCacheStatus",
            MinecraftPacketIds.OnScreenTextureAnimation => "OnScreenTextureAnimation",
            MinecraftPacketIds.MapCreateLockedCopy => "MapCreateLockedCopy",
            MinecraftPacketIds.StructureTemplateDataExportRequest => "StructureTemplateDataExportRequest",
            MinecraftPacketIds.StructureTemplateDataExportResponse => "StructureTemplateDataExportResponse",
            MinecraftPacketIds.ClientCacheBlobStatusPacket => "ClientCacheBlobStatusPacket",
            MinecraftPacketIds.ClientCacheMissResponsePacket => "ClientCacheMissResponsePacket",
            MinecraftPacketIds.EducationSettingsPacket => "EducationSettingsPacket",
            MinecraftPacketIds.Emote => "Emote",
            MinecraftPacketIds.MultiplayerSettingsPacket => "MultiplayerSettingsPacket",
            MinecraftPacketIds.SettingsCommandPacket => "SettingsCommandPacket",
            MinecraftPacketIds.AnvilDamage => "AnvilDamage",
            MinecraftPacketIds.CompletedUsingItem => "CompletedUsingItem",
            MinecraftPacketIds.NetworkSettings => "NetworkSettings",
            MinecraftPacketIds.PlayerAuthInputPacket => "PlayerAuthInputPacket",
            MinecraftPacketIds.CreativeContent => "CreativeContent",
            MinecraftPacketIds.PlayerEnchantOptions => "PlayerEnchantOptions",
            MinecraftPacketIds.ItemStackRequest => "ItemStackRequest",
            MinecraftPacketIds.ItemStackResponse => "ItemStackResponse",
            MinecraftPacketIds.PlayerArmorDamage => "PlayerArmorDamage",
            MinecraftPacketIds.CodeBuilderPacket => "CodeBuilderPacket",
            MinecraftPacketIds.UpdatePlayerGameType => "UpdatePlayerGameType",
            MinecraftPacketIds.EmoteList => "EmoteList",
            MinecraftPacketIds.PositionTrackingDBServerBroadcast => "PositionTrackingDBServerBroadcast",
            MinecraftPacketIds.PositionTrackingDBClientRequest => "PositionTrackingDBClientRequest",
            MinecraftPacketIds.DebugInfoPacket => "DebugInfoPacket",
            MinecraftPacketIds.PacketViolationWarning => "PacketViolationWarning",
            MinecraftPacketIds.MotionPredictionHints => "MotionPredictionHints",
            MinecraftPacketIds.TriggerAnimation => "TriggerAnimation",
            MinecraftPacketIds.CameraShake => "CameraShake",
            MinecraftPacketIds.PlayerFogSetting => "PlayerFogSetting",
            MinecraftPacketIds.CorrectPlayerMovePredictionPacket => "CorrectPlayerMovePredictionPacket",
            MinecraftPacketIds.ItemRegistryPacket => "ItemRegistryPacket",
            MinecraftPacketIds.ClientBoundDebugRendererPacket => "ClientBoundDebugRendererPacket",
            MinecraftPacketIds.SyncActorProperty => "SyncActorProperty",
            MinecraftPacketIds.AddVolumeEntityPacket => "AddVolumeEntityPacket",
            MinecraftPacketIds.RemoveVolumeEntityPacket => "RemoveVolumeEntityPacket",
            MinecraftPacketIds.SimulationTypePacket => "SimulationTypePacket",
            MinecraftPacketIds.NpcDialoguePacket => "NpcDialoguePacket",
            MinecraftPacketIds.EduUriResourcePacket => "EduUriResourcePacket",
            MinecraftPacketIds.CreatePhotoPacket => "CreatePhotoPacket",
            MinecraftPacketIds.UpdateSubChunkBlocks => "UpdateSubChunkBlocks",
            MinecraftPacketIds.SubChunkPacket => "SubChunkPacket",
            MinecraftPacketIds.SubChunkRequestPacket => "SubChunkRequestPacket",
            MinecraftPacketIds.PlayerStartItemCooldown => "PlayerStartItemCooldown",
            MinecraftPacketIds.ScriptMessagePacket => "ScriptMessagePacket",
            MinecraftPacketIds.CodeBuilderSourcePacket => "CodeBuilderSourcePacket",
            MinecraftPacketIds.TickingAreasLoadStatus => "TickingAreasLoadStatus",
            MinecraftPacketIds.DimensionDataPacket => "DimensionDataPacket",
            MinecraftPacketIds.AgentAction => "AgentAction",
            MinecraftPacketIds.ChangeMobProperty => "ChangeMobProperty",
            MinecraftPacketIds.LessonProgressPacket => "LessonProgressPacket",
            MinecraftPacketIds.RequestAbilityPacket => "RequestAbilityPacket",
            MinecraftPacketIds.RequestPermissionsPacket => "RequestPermissionsPacket",
            MinecraftPacketIds.ToastRequest => "ToastRequest",
            MinecraftPacketIds.UpdateAbilitiesPacket => "UpdateAbilitiesPacket",
            MinecraftPacketIds.UpdateAdventureSettingsPacket => "UpdateAdventureSettingsPacket",
            MinecraftPacketIds.DeathInfo => "DeathInfo",
            MinecraftPacketIds.EditorNetworkPacket => "EditorNetworkPacket",
            MinecraftPacketIds.FeatureRegistryPacket => "FeatureRegistryPacket",
            MinecraftPacketIds.ServerStats => "ServerStats",
            MinecraftPacketIds.RequestNetworkSettings => "RequestNetworkSettings",
            MinecraftPacketIds.GameTestRequestPacket => "GameTestRequestPacket",
            MinecraftPacketIds.GameTestResultsPacket => "GameTestResultsPacket",
            MinecraftPacketIds.PlayerClientInputPermissions => "PlayerClientInputPermissions",
            MinecraftPacketIds.CameraPresets => "CameraPresets",
            MinecraftPacketIds.UnlockedRecipes => "UnlockedRecipes",
            MinecraftPacketIds.TitleSpecificPacketsStart => "TitleSpecificPacketsStart",
            MinecraftPacketIds.TitleSpecificPacketsEnd => "TitleSpecificPacketsEnd",
            MinecraftPacketIds.CameraInstruction => "CameraInstruction",
            MinecraftPacketIds.TrimData => "TrimData",
            MinecraftPacketIds.OpenSign => "OpenSign",
            MinecraftPacketIds.AgentAnimation => "AgentAnimation",
            MinecraftPacketIds.RefreshEntitlementsPacket => "RefreshEntitlementsPacket",
            MinecraftPacketIds.PlayerToggleCrafterSlotRequestPacket => "PlayerToggleCrafterSlotRequestPacket",
            MinecraftPacketIds.SetPlayerInventoryOptions => "SetPlayerInventoryOptions",
            MinecraftPacketIds.SetHudPacket => "SetHudPacket",
            MinecraftPacketIds.AwardAchievementPacket => "AwardAchievementPacket",
            MinecraftPacketIds.ClientboundCloseScreen => "ClientboundCloseScreen",
            MinecraftPacketIds.ServerboundLoadingScreenPacket => "ServerboundLoadingScreenPacket",
            MinecraftPacketIds.JigsawStructureDataPacket => "JigsawStructureDataPacket",
            MinecraftPacketIds.CurrentStructureFeaturePacket => "CurrentStructureFeaturePacket",
            MinecraftPacketIds.ServerboundDiagnosticsPacket => "ServerboundDiagnosticsPacket",
            MinecraftPacketIds.CameraAimAssist => "CameraAimAssist",
            MinecraftPacketIds.ContainerRegistryCleanup => "ContainerRegistryCleanup",
            MinecraftPacketIds.MovementEffect => "MovementEffect",
            MinecraftPacketIds.CameraAimAssistActorPriority => "CameraAimAssistActorPriority",
            MinecraftPacketIds.CameraAimAssistPresets => "CameraAimAssistPresets",
            MinecraftPacketIds.ClientCameraAimAssist => "ClientCameraAimAssist",
            MinecraftPacketIds.ClientMovementPredictionSyncPacket => "ClientMovementPredictionSyncPacket",
            MinecraftPacketIds.UpdateClientOptions => "UpdateClientOptions",
            MinecraftPacketIds.PlayerVideoCapturePacket => "PlayerVideoCapturePacket",
            MinecraftPacketIds.PlayerUpdateEntityOverridesPacket => "PlayerUpdateEntityOverridesPacket",
            MinecraftPacketIds.PlayerLocation => "PlayerLocation",
            MinecraftPacketIds.SyncWorldClocks => "SyncWorldClocks",
            MinecraftPacketIds.SendPartyDestinationCookie => "SendPartyDestinationCookie",
            MinecraftPacketIds.PartyDestinationCookieResponse => "PartyDestinationCookieResponse",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown MinecraftPacketIds value.")
        };
    }

    public static MinecraftPacketIds FromProtocolString(string value) {
        return value switch {
            "KeepAlive" => MinecraftPacketIds.KeepAlive,
            "Login" => MinecraftPacketIds.Login,
            "PlayStatus" => MinecraftPacketIds.PlayStatus,
            "ServerToClientHandshake" => MinecraftPacketIds.ServerToClientHandshake,
            "ClientToServerHandshake" => MinecraftPacketIds.ClientToServerHandshake,
            "Disconnect" => MinecraftPacketIds.Disconnect,
            "ResourcePacksInfo" => MinecraftPacketIds.ResourcePacksInfo,
            "ResourcePackStack" => MinecraftPacketIds.ResourcePackStack,
            "ResourcePackClientResponse" => MinecraftPacketIds.ResourcePackClientResponse,
            "Text" => MinecraftPacketIds.Text,
            "SetTime" => MinecraftPacketIds.SetTime,
            "StartGame" => MinecraftPacketIds.StartGame,
            "AddPlayer" => MinecraftPacketIds.AddPlayer,
            "AddActor" => MinecraftPacketIds.AddActor,
            "RemoveActor" => MinecraftPacketIds.RemoveActor,
            "AddItemActor" => MinecraftPacketIds.AddItemActor,
            "ServerPlayerPostMovePosition" => MinecraftPacketIds.ServerPlayerPostMovePosition,
            "TakeItemActor" => MinecraftPacketIds.TakeItemActor,
            "MoveAbsoluteActor" => MinecraftPacketIds.MoveAbsoluteActor,
            "MovePlayer" => MinecraftPacketIds.MovePlayer,
            "UpdateBlock" => MinecraftPacketIds.UpdateBlock,
            "AddPainting" => MinecraftPacketIds.AddPainting,
            "LevelEvent" => MinecraftPacketIds.LevelEvent,
            "TileEvent" => MinecraftPacketIds.TileEvent,
            "ActorEvent" => MinecraftPacketIds.ActorEvent,
            "MobEffect" => MinecraftPacketIds.MobEffect,
            "UpdateAttributes" => MinecraftPacketIds.UpdateAttributes,
            "InventoryTransaction" => MinecraftPacketIds.InventoryTransaction,
            "PlayerEquipment" => MinecraftPacketIds.PlayerEquipment,
            "MobArmorEquipment" => MinecraftPacketIds.MobArmorEquipment,
            "Interact" => MinecraftPacketIds.Interact,
            "BlockPickRequest" => MinecraftPacketIds.BlockPickRequest,
            "ActorPickRequest" => MinecraftPacketIds.ActorPickRequest,
            "PlayerAction" => MinecraftPacketIds.PlayerAction,
            "HurtArmor" => MinecraftPacketIds.HurtArmor,
            "SetActorData" => MinecraftPacketIds.SetActorData,
            "SetActorMotion" => MinecraftPacketIds.SetActorMotion,
            "SetActorLink" => MinecraftPacketIds.SetActorLink,
            "SetHealth" => MinecraftPacketIds.SetHealth,
            "SetSpawnPosition" => MinecraftPacketIds.SetSpawnPosition,
            "Animate" => MinecraftPacketIds.Animate,
            "Respawn" => MinecraftPacketIds.Respawn,
            "ContainerOpen" => MinecraftPacketIds.ContainerOpen,
            "ContainerClose" => MinecraftPacketIds.ContainerClose,
            "PlayerHotbar" => MinecraftPacketIds.PlayerHotbar,
            "InventoryContent" => MinecraftPacketIds.InventoryContent,
            "InventorySlot" => MinecraftPacketIds.InventorySlot,
            "ContainerSetData" => MinecraftPacketIds.ContainerSetData,
            "CraftingData" => MinecraftPacketIds.CraftingData,
            "GuiDataPickItem" => MinecraftPacketIds.GuiDataPickItem,
            "BlockActorData" => MinecraftPacketIds.BlockActorData,
            "FullChunkData" => MinecraftPacketIds.FullChunkData,
            "SetCommandsEnabled" => MinecraftPacketIds.SetCommandsEnabled,
            "SetDifficulty" => MinecraftPacketIds.SetDifficulty,
            "ChangeDimension" => MinecraftPacketIds.ChangeDimension,
            "SetPlayerGameType" => MinecraftPacketIds.SetPlayerGameType,
            "PlayerList" => MinecraftPacketIds.PlayerList,
            "SimpleEvent" => MinecraftPacketIds.SimpleEvent,
            "LegacyTelemetryEvent" => MinecraftPacketIds.LegacyTelemetryEvent,
            "SpawnExperienceOrb" => MinecraftPacketIds.SpawnExperienceOrb,
            "MapData" => MinecraftPacketIds.MapData,
            "MapInfoRequest" => MinecraftPacketIds.MapInfoRequest,
            "RequestChunkRadius" => MinecraftPacketIds.RequestChunkRadius,
            "ChunkRadiusUpdated" => MinecraftPacketIds.ChunkRadiusUpdated,
            "GameRulesChanged" => MinecraftPacketIds.GameRulesChanged,
            "Camera" => MinecraftPacketIds.Camera,
            "BossEvent" => MinecraftPacketIds.BossEvent,
            "ShowCredits" => MinecraftPacketIds.ShowCredits,
            "AvailableCommands" => MinecraftPacketIds.AvailableCommands,
            "CommandRequest" => MinecraftPacketIds.CommandRequest,
            "CommandBlockUpdate" => MinecraftPacketIds.CommandBlockUpdate,
            "CommandOutput" => MinecraftPacketIds.CommandOutput,
            "UpdateTrade" => MinecraftPacketIds.UpdateTrade,
            "UpdateEquip" => MinecraftPacketIds.UpdateEquip,
            "ResourcePackDataInfo" => MinecraftPacketIds.ResourcePackDataInfo,
            "ResourcePackChunkData" => MinecraftPacketIds.ResourcePackChunkData,
            "ResourcePackChunkRequest" => MinecraftPacketIds.ResourcePackChunkRequest,
            "Transfer" => MinecraftPacketIds.Transfer,
            "PlaySound" => MinecraftPacketIds.PlaySound,
            "StopSound" => MinecraftPacketIds.StopSound,
            "SetTitle" => MinecraftPacketIds.SetTitle,
            "AddBehaviorTree" => MinecraftPacketIds.AddBehaviorTree,
            "StructureBlockUpdate" => MinecraftPacketIds.StructureBlockUpdate,
            "ShowStoreOffer" => MinecraftPacketIds.ShowStoreOffer,
            "PurchaseReceipt" => MinecraftPacketIds.PurchaseReceipt,
            "PlayerSkin" => MinecraftPacketIds.PlayerSkin,
            "SubclientLogin" => MinecraftPacketIds.SubclientLogin,
            "AutomationClientConnect" => MinecraftPacketIds.AutomationClientConnect,
            "SetLastHurtBy" => MinecraftPacketIds.SetLastHurtBy,
            "BookEdit" => MinecraftPacketIds.BookEdit,
            "NPCRequest" => MinecraftPacketIds.NPCRequest,
            "PhotoTransfer" => MinecraftPacketIds.PhotoTransfer,
            "ShowModalForm" => MinecraftPacketIds.ShowModalForm,
            "ModalFormResponse" => MinecraftPacketIds.ModalFormResponse,
            "ServerSettingsRequest" => MinecraftPacketIds.ServerSettingsRequest,
            "ServerSettingsResponse" => MinecraftPacketIds.ServerSettingsResponse,
            "ShowProfile" => MinecraftPacketIds.ShowProfile,
            "SetDefaultGameType" => MinecraftPacketIds.SetDefaultGameType,
            "RemoveObjective" => MinecraftPacketIds.RemoveObjective,
            "SetDisplayObjective" => MinecraftPacketIds.SetDisplayObjective,
            "SetScore" => MinecraftPacketIds.SetScore,
            "LabTable" => MinecraftPacketIds.LabTable,
            "UpdateBlockSynced" => MinecraftPacketIds.UpdateBlockSynced,
            "MoveDeltaActor" => MinecraftPacketIds.MoveDeltaActor,
            "SetScoreboardIdentity" => MinecraftPacketIds.SetScoreboardIdentity,
            "SetLocalPlayerAsInit" => MinecraftPacketIds.SetLocalPlayerAsInit,
            "UpdateSoftEnum" => MinecraftPacketIds.UpdateSoftEnum,
            "Ping" => MinecraftPacketIds.Ping,
            "ScriptCustomEvent" => MinecraftPacketIds.ScriptCustomEvent,
            "SpawnParticleEffect" => MinecraftPacketIds.SpawnParticleEffect,
            "AvailableActorIDList" => MinecraftPacketIds.AvailableActorIDList,
            "NetworkChunkPublisherUpdate" => MinecraftPacketIds.NetworkChunkPublisherUpdate,
            "BiomeDefinitionList" => MinecraftPacketIds.BiomeDefinitionList,
            "LevelSoundEvent" => MinecraftPacketIds.LevelSoundEvent,
            "LevelEventGeneric" => MinecraftPacketIds.LevelEventGeneric,
            "LecternUpdate" => MinecraftPacketIds.LecternUpdate,
            "ClientCacheStatus" => MinecraftPacketIds.ClientCacheStatus,
            "OnScreenTextureAnimation" => MinecraftPacketIds.OnScreenTextureAnimation,
            "MapCreateLockedCopy" => MinecraftPacketIds.MapCreateLockedCopy,
            "StructureTemplateDataExportRequest" => MinecraftPacketIds.StructureTemplateDataExportRequest,
            "StructureTemplateDataExportResponse" => MinecraftPacketIds.StructureTemplateDataExportResponse,
            "ClientCacheBlobStatusPacket" => MinecraftPacketIds.ClientCacheBlobStatusPacket,
            "ClientCacheMissResponsePacket" => MinecraftPacketIds.ClientCacheMissResponsePacket,
            "EducationSettingsPacket" => MinecraftPacketIds.EducationSettingsPacket,
            "Emote" => MinecraftPacketIds.Emote,
            "MultiplayerSettingsPacket" => MinecraftPacketIds.MultiplayerSettingsPacket,
            "SettingsCommandPacket" => MinecraftPacketIds.SettingsCommandPacket,
            "AnvilDamage" => MinecraftPacketIds.AnvilDamage,
            "CompletedUsingItem" => MinecraftPacketIds.CompletedUsingItem,
            "NetworkSettings" => MinecraftPacketIds.NetworkSettings,
            "PlayerAuthInputPacket" => MinecraftPacketIds.PlayerAuthInputPacket,
            "CreativeContent" => MinecraftPacketIds.CreativeContent,
            "PlayerEnchantOptions" => MinecraftPacketIds.PlayerEnchantOptions,
            "ItemStackRequest" => MinecraftPacketIds.ItemStackRequest,
            "ItemStackResponse" => MinecraftPacketIds.ItemStackResponse,
            "PlayerArmorDamage" => MinecraftPacketIds.PlayerArmorDamage,
            "CodeBuilderPacket" => MinecraftPacketIds.CodeBuilderPacket,
            "UpdatePlayerGameType" => MinecraftPacketIds.UpdatePlayerGameType,
            "EmoteList" => MinecraftPacketIds.EmoteList,
            "PositionTrackingDBServerBroadcast" => MinecraftPacketIds.PositionTrackingDBServerBroadcast,
            "PositionTrackingDBClientRequest" => MinecraftPacketIds.PositionTrackingDBClientRequest,
            "DebugInfoPacket" => MinecraftPacketIds.DebugInfoPacket,
            "PacketViolationWarning" => MinecraftPacketIds.PacketViolationWarning,
            "MotionPredictionHints" => MinecraftPacketIds.MotionPredictionHints,
            "TriggerAnimation" => MinecraftPacketIds.TriggerAnimation,
            "CameraShake" => MinecraftPacketIds.CameraShake,
            "PlayerFogSetting" => MinecraftPacketIds.PlayerFogSetting,
            "CorrectPlayerMovePredictionPacket" => MinecraftPacketIds.CorrectPlayerMovePredictionPacket,
            "ItemRegistryPacket" => MinecraftPacketIds.ItemRegistryPacket,
            "ClientBoundDebugRendererPacket" => MinecraftPacketIds.ClientBoundDebugRendererPacket,
            "SyncActorProperty" => MinecraftPacketIds.SyncActorProperty,
            "AddVolumeEntityPacket" => MinecraftPacketIds.AddVolumeEntityPacket,
            "RemoveVolumeEntityPacket" => MinecraftPacketIds.RemoveVolumeEntityPacket,
            "SimulationTypePacket" => MinecraftPacketIds.SimulationTypePacket,
            "NpcDialoguePacket" => MinecraftPacketIds.NpcDialoguePacket,
            "EduUriResourcePacket" => MinecraftPacketIds.EduUriResourcePacket,
            "CreatePhotoPacket" => MinecraftPacketIds.CreatePhotoPacket,
            "UpdateSubChunkBlocks" => MinecraftPacketIds.UpdateSubChunkBlocks,
            "SubChunkPacket" => MinecraftPacketIds.SubChunkPacket,
            "SubChunkRequestPacket" => MinecraftPacketIds.SubChunkRequestPacket,
            "PlayerStartItemCooldown" => MinecraftPacketIds.PlayerStartItemCooldown,
            "ScriptMessagePacket" => MinecraftPacketIds.ScriptMessagePacket,
            "CodeBuilderSourcePacket" => MinecraftPacketIds.CodeBuilderSourcePacket,
            "TickingAreasLoadStatus" => MinecraftPacketIds.TickingAreasLoadStatus,
            "DimensionDataPacket" => MinecraftPacketIds.DimensionDataPacket,
            "AgentAction" => MinecraftPacketIds.AgentAction,
            "ChangeMobProperty" => MinecraftPacketIds.ChangeMobProperty,
            "LessonProgressPacket" => MinecraftPacketIds.LessonProgressPacket,
            "RequestAbilityPacket" => MinecraftPacketIds.RequestAbilityPacket,
            "RequestPermissionsPacket" => MinecraftPacketIds.RequestPermissionsPacket,
            "ToastRequest" => MinecraftPacketIds.ToastRequest,
            "UpdateAbilitiesPacket" => MinecraftPacketIds.UpdateAbilitiesPacket,
            "UpdateAdventureSettingsPacket" => MinecraftPacketIds.UpdateAdventureSettingsPacket,
            "DeathInfo" => MinecraftPacketIds.DeathInfo,
            "EditorNetworkPacket" => MinecraftPacketIds.EditorNetworkPacket,
            "FeatureRegistryPacket" => MinecraftPacketIds.FeatureRegistryPacket,
            "ServerStats" => MinecraftPacketIds.ServerStats,
            "RequestNetworkSettings" => MinecraftPacketIds.RequestNetworkSettings,
            "GameTestRequestPacket" => MinecraftPacketIds.GameTestRequestPacket,
            "GameTestResultsPacket" => MinecraftPacketIds.GameTestResultsPacket,
            "PlayerClientInputPermissions" => MinecraftPacketIds.PlayerClientInputPermissions,
            "CameraPresets" => MinecraftPacketIds.CameraPresets,
            "UnlockedRecipes" => MinecraftPacketIds.UnlockedRecipes,
            "TitleSpecificPacketsStart" => MinecraftPacketIds.TitleSpecificPacketsStart,
            "TitleSpecificPacketsEnd" => MinecraftPacketIds.TitleSpecificPacketsEnd,
            "CameraInstruction" => MinecraftPacketIds.CameraInstruction,
            "TrimData" => MinecraftPacketIds.TrimData,
            "OpenSign" => MinecraftPacketIds.OpenSign,
            "AgentAnimation" => MinecraftPacketIds.AgentAnimation,
            "RefreshEntitlementsPacket" => MinecraftPacketIds.RefreshEntitlementsPacket,
            "PlayerToggleCrafterSlotRequestPacket" => MinecraftPacketIds.PlayerToggleCrafterSlotRequestPacket,
            "SetPlayerInventoryOptions" => MinecraftPacketIds.SetPlayerInventoryOptions,
            "SetHudPacket" => MinecraftPacketIds.SetHudPacket,
            "AwardAchievementPacket" => MinecraftPacketIds.AwardAchievementPacket,
            "ClientboundCloseScreen" => MinecraftPacketIds.ClientboundCloseScreen,
            "ServerboundLoadingScreenPacket" => MinecraftPacketIds.ServerboundLoadingScreenPacket,
            "JigsawStructureDataPacket" => MinecraftPacketIds.JigsawStructureDataPacket,
            "CurrentStructureFeaturePacket" => MinecraftPacketIds.CurrentStructureFeaturePacket,
            "ServerboundDiagnosticsPacket" => MinecraftPacketIds.ServerboundDiagnosticsPacket,
            "CameraAimAssist" => MinecraftPacketIds.CameraAimAssist,
            "ContainerRegistryCleanup" => MinecraftPacketIds.ContainerRegistryCleanup,
            "MovementEffect" => MinecraftPacketIds.MovementEffect,
            "CameraAimAssistActorPriority" => MinecraftPacketIds.CameraAimAssistActorPriority,
            "CameraAimAssistPresets" => MinecraftPacketIds.CameraAimAssistPresets,
            "ClientCameraAimAssist" => MinecraftPacketIds.ClientCameraAimAssist,
            "ClientMovementPredictionSyncPacket" => MinecraftPacketIds.ClientMovementPredictionSyncPacket,
            "UpdateClientOptions" => MinecraftPacketIds.UpdateClientOptions,
            "PlayerVideoCapturePacket" => MinecraftPacketIds.PlayerVideoCapturePacket,
            "PlayerUpdateEntityOverridesPacket" => MinecraftPacketIds.PlayerUpdateEntityOverridesPacket,
            "PlayerLocation" => MinecraftPacketIds.PlayerLocation,
            "SyncWorldClocks" => MinecraftPacketIds.SyncWorldClocks,
            "SendPartyDestinationCookie" => MinecraftPacketIds.SendPartyDestinationCookie,
            "PartyDestinationCookieResponse" => MinecraftPacketIds.PartyDestinationCookieResponse,
            _ => throw new ArgumentException($"Unknown MinecraftPacketIds protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out MinecraftPacketIds result) {
        switch (value) {
            case "KeepAlive":
                result = MinecraftPacketIds.KeepAlive;
                return true;
            case "Login":
                result = MinecraftPacketIds.Login;
                return true;
            case "PlayStatus":
                result = MinecraftPacketIds.PlayStatus;
                return true;
            case "ServerToClientHandshake":
                result = MinecraftPacketIds.ServerToClientHandshake;
                return true;
            case "ClientToServerHandshake":
                result = MinecraftPacketIds.ClientToServerHandshake;
                return true;
            case "Disconnect":
                result = MinecraftPacketIds.Disconnect;
                return true;
            case "ResourcePacksInfo":
                result = MinecraftPacketIds.ResourcePacksInfo;
                return true;
            case "ResourcePackStack":
                result = MinecraftPacketIds.ResourcePackStack;
                return true;
            case "ResourcePackClientResponse":
                result = MinecraftPacketIds.ResourcePackClientResponse;
                return true;
            case "Text":
                result = MinecraftPacketIds.Text;
                return true;
            case "SetTime":
                result = MinecraftPacketIds.SetTime;
                return true;
            case "StartGame":
                result = MinecraftPacketIds.StartGame;
                return true;
            case "AddPlayer":
                result = MinecraftPacketIds.AddPlayer;
                return true;
            case "AddActor":
                result = MinecraftPacketIds.AddActor;
                return true;
            case "RemoveActor":
                result = MinecraftPacketIds.RemoveActor;
                return true;
            case "AddItemActor":
                result = MinecraftPacketIds.AddItemActor;
                return true;
            case "ServerPlayerPostMovePosition":
                result = MinecraftPacketIds.ServerPlayerPostMovePosition;
                return true;
            case "TakeItemActor":
                result = MinecraftPacketIds.TakeItemActor;
                return true;
            case "MoveAbsoluteActor":
                result = MinecraftPacketIds.MoveAbsoluteActor;
                return true;
            case "MovePlayer":
                result = MinecraftPacketIds.MovePlayer;
                return true;
            case "UpdateBlock":
                result = MinecraftPacketIds.UpdateBlock;
                return true;
            case "AddPainting":
                result = MinecraftPacketIds.AddPainting;
                return true;
            case "LevelEvent":
                result = MinecraftPacketIds.LevelEvent;
                return true;
            case "TileEvent":
                result = MinecraftPacketIds.TileEvent;
                return true;
            case "ActorEvent":
                result = MinecraftPacketIds.ActorEvent;
                return true;
            case "MobEffect":
                result = MinecraftPacketIds.MobEffect;
                return true;
            case "UpdateAttributes":
                result = MinecraftPacketIds.UpdateAttributes;
                return true;
            case "InventoryTransaction":
                result = MinecraftPacketIds.InventoryTransaction;
                return true;
            case "PlayerEquipment":
                result = MinecraftPacketIds.PlayerEquipment;
                return true;
            case "MobArmorEquipment":
                result = MinecraftPacketIds.MobArmorEquipment;
                return true;
            case "Interact":
                result = MinecraftPacketIds.Interact;
                return true;
            case "BlockPickRequest":
                result = MinecraftPacketIds.BlockPickRequest;
                return true;
            case "ActorPickRequest":
                result = MinecraftPacketIds.ActorPickRequest;
                return true;
            case "PlayerAction":
                result = MinecraftPacketIds.PlayerAction;
                return true;
            case "HurtArmor":
                result = MinecraftPacketIds.HurtArmor;
                return true;
            case "SetActorData":
                result = MinecraftPacketIds.SetActorData;
                return true;
            case "SetActorMotion":
                result = MinecraftPacketIds.SetActorMotion;
                return true;
            case "SetActorLink":
                result = MinecraftPacketIds.SetActorLink;
                return true;
            case "SetHealth":
                result = MinecraftPacketIds.SetHealth;
                return true;
            case "SetSpawnPosition":
                result = MinecraftPacketIds.SetSpawnPosition;
                return true;
            case "Animate":
                result = MinecraftPacketIds.Animate;
                return true;
            case "Respawn":
                result = MinecraftPacketIds.Respawn;
                return true;
            case "ContainerOpen":
                result = MinecraftPacketIds.ContainerOpen;
                return true;
            case "ContainerClose":
                result = MinecraftPacketIds.ContainerClose;
                return true;
            case "PlayerHotbar":
                result = MinecraftPacketIds.PlayerHotbar;
                return true;
            case "InventoryContent":
                result = MinecraftPacketIds.InventoryContent;
                return true;
            case "InventorySlot":
                result = MinecraftPacketIds.InventorySlot;
                return true;
            case "ContainerSetData":
                result = MinecraftPacketIds.ContainerSetData;
                return true;
            case "CraftingData":
                result = MinecraftPacketIds.CraftingData;
                return true;
            case "GuiDataPickItem":
                result = MinecraftPacketIds.GuiDataPickItem;
                return true;
            case "BlockActorData":
                result = MinecraftPacketIds.BlockActorData;
                return true;
            case "FullChunkData":
                result = MinecraftPacketIds.FullChunkData;
                return true;
            case "SetCommandsEnabled":
                result = MinecraftPacketIds.SetCommandsEnabled;
                return true;
            case "SetDifficulty":
                result = MinecraftPacketIds.SetDifficulty;
                return true;
            case "ChangeDimension":
                result = MinecraftPacketIds.ChangeDimension;
                return true;
            case "SetPlayerGameType":
                result = MinecraftPacketIds.SetPlayerGameType;
                return true;
            case "PlayerList":
                result = MinecraftPacketIds.PlayerList;
                return true;
            case "SimpleEvent":
                result = MinecraftPacketIds.SimpleEvent;
                return true;
            case "LegacyTelemetryEvent":
                result = MinecraftPacketIds.LegacyTelemetryEvent;
                return true;
            case "SpawnExperienceOrb":
                result = MinecraftPacketIds.SpawnExperienceOrb;
                return true;
            case "MapData":
                result = MinecraftPacketIds.MapData;
                return true;
            case "MapInfoRequest":
                result = MinecraftPacketIds.MapInfoRequest;
                return true;
            case "RequestChunkRadius":
                result = MinecraftPacketIds.RequestChunkRadius;
                return true;
            case "ChunkRadiusUpdated":
                result = MinecraftPacketIds.ChunkRadiusUpdated;
                return true;
            case "GameRulesChanged":
                result = MinecraftPacketIds.GameRulesChanged;
                return true;
            case "Camera":
                result = MinecraftPacketIds.Camera;
                return true;
            case "BossEvent":
                result = MinecraftPacketIds.BossEvent;
                return true;
            case "ShowCredits":
                result = MinecraftPacketIds.ShowCredits;
                return true;
            case "AvailableCommands":
                result = MinecraftPacketIds.AvailableCommands;
                return true;
            case "CommandRequest":
                result = MinecraftPacketIds.CommandRequest;
                return true;
            case "CommandBlockUpdate":
                result = MinecraftPacketIds.CommandBlockUpdate;
                return true;
            case "CommandOutput":
                result = MinecraftPacketIds.CommandOutput;
                return true;
            case "UpdateTrade":
                result = MinecraftPacketIds.UpdateTrade;
                return true;
            case "UpdateEquip":
                result = MinecraftPacketIds.UpdateEquip;
                return true;
            case "ResourcePackDataInfo":
                result = MinecraftPacketIds.ResourcePackDataInfo;
                return true;
            case "ResourcePackChunkData":
                result = MinecraftPacketIds.ResourcePackChunkData;
                return true;
            case "ResourcePackChunkRequest":
                result = MinecraftPacketIds.ResourcePackChunkRequest;
                return true;
            case "Transfer":
                result = MinecraftPacketIds.Transfer;
                return true;
            case "PlaySound":
                result = MinecraftPacketIds.PlaySound;
                return true;
            case "StopSound":
                result = MinecraftPacketIds.StopSound;
                return true;
            case "SetTitle":
                result = MinecraftPacketIds.SetTitle;
                return true;
            case "AddBehaviorTree":
                result = MinecraftPacketIds.AddBehaviorTree;
                return true;
            case "StructureBlockUpdate":
                result = MinecraftPacketIds.StructureBlockUpdate;
                return true;
            case "ShowStoreOffer":
                result = MinecraftPacketIds.ShowStoreOffer;
                return true;
            case "PurchaseReceipt":
                result = MinecraftPacketIds.PurchaseReceipt;
                return true;
            case "PlayerSkin":
                result = MinecraftPacketIds.PlayerSkin;
                return true;
            case "SubclientLogin":
                result = MinecraftPacketIds.SubclientLogin;
                return true;
            case "AutomationClientConnect":
                result = MinecraftPacketIds.AutomationClientConnect;
                return true;
            case "SetLastHurtBy":
                result = MinecraftPacketIds.SetLastHurtBy;
                return true;
            case "BookEdit":
                result = MinecraftPacketIds.BookEdit;
                return true;
            case "NPCRequest":
                result = MinecraftPacketIds.NPCRequest;
                return true;
            case "PhotoTransfer":
                result = MinecraftPacketIds.PhotoTransfer;
                return true;
            case "ShowModalForm":
                result = MinecraftPacketIds.ShowModalForm;
                return true;
            case "ModalFormResponse":
                result = MinecraftPacketIds.ModalFormResponse;
                return true;
            case "ServerSettingsRequest":
                result = MinecraftPacketIds.ServerSettingsRequest;
                return true;
            case "ServerSettingsResponse":
                result = MinecraftPacketIds.ServerSettingsResponse;
                return true;
            case "ShowProfile":
                result = MinecraftPacketIds.ShowProfile;
                return true;
            case "SetDefaultGameType":
                result = MinecraftPacketIds.SetDefaultGameType;
                return true;
            case "RemoveObjective":
                result = MinecraftPacketIds.RemoveObjective;
                return true;
            case "SetDisplayObjective":
                result = MinecraftPacketIds.SetDisplayObjective;
                return true;
            case "SetScore":
                result = MinecraftPacketIds.SetScore;
                return true;
            case "LabTable":
                result = MinecraftPacketIds.LabTable;
                return true;
            case "UpdateBlockSynced":
                result = MinecraftPacketIds.UpdateBlockSynced;
                return true;
            case "MoveDeltaActor":
                result = MinecraftPacketIds.MoveDeltaActor;
                return true;
            case "SetScoreboardIdentity":
                result = MinecraftPacketIds.SetScoreboardIdentity;
                return true;
            case "SetLocalPlayerAsInit":
                result = MinecraftPacketIds.SetLocalPlayerAsInit;
                return true;
            case "UpdateSoftEnum":
                result = MinecraftPacketIds.UpdateSoftEnum;
                return true;
            case "Ping":
                result = MinecraftPacketIds.Ping;
                return true;
            case "ScriptCustomEvent":
                result = MinecraftPacketIds.ScriptCustomEvent;
                return true;
            case "SpawnParticleEffect":
                result = MinecraftPacketIds.SpawnParticleEffect;
                return true;
            case "AvailableActorIDList":
                result = MinecraftPacketIds.AvailableActorIDList;
                return true;
            case "NetworkChunkPublisherUpdate":
                result = MinecraftPacketIds.NetworkChunkPublisherUpdate;
                return true;
            case "BiomeDefinitionList":
                result = MinecraftPacketIds.BiomeDefinitionList;
                return true;
            case "LevelSoundEvent":
                result = MinecraftPacketIds.LevelSoundEvent;
                return true;
            case "LevelEventGeneric":
                result = MinecraftPacketIds.LevelEventGeneric;
                return true;
            case "LecternUpdate":
                result = MinecraftPacketIds.LecternUpdate;
                return true;
            case "ClientCacheStatus":
                result = MinecraftPacketIds.ClientCacheStatus;
                return true;
            case "OnScreenTextureAnimation":
                result = MinecraftPacketIds.OnScreenTextureAnimation;
                return true;
            case "MapCreateLockedCopy":
                result = MinecraftPacketIds.MapCreateLockedCopy;
                return true;
            case "StructureTemplateDataExportRequest":
                result = MinecraftPacketIds.StructureTemplateDataExportRequest;
                return true;
            case "StructureTemplateDataExportResponse":
                result = MinecraftPacketIds.StructureTemplateDataExportResponse;
                return true;
            case "ClientCacheBlobStatusPacket":
                result = MinecraftPacketIds.ClientCacheBlobStatusPacket;
                return true;
            case "ClientCacheMissResponsePacket":
                result = MinecraftPacketIds.ClientCacheMissResponsePacket;
                return true;
            case "EducationSettingsPacket":
                result = MinecraftPacketIds.EducationSettingsPacket;
                return true;
            case "Emote":
                result = MinecraftPacketIds.Emote;
                return true;
            case "MultiplayerSettingsPacket":
                result = MinecraftPacketIds.MultiplayerSettingsPacket;
                return true;
            case "SettingsCommandPacket":
                result = MinecraftPacketIds.SettingsCommandPacket;
                return true;
            case "AnvilDamage":
                result = MinecraftPacketIds.AnvilDamage;
                return true;
            case "CompletedUsingItem":
                result = MinecraftPacketIds.CompletedUsingItem;
                return true;
            case "NetworkSettings":
                result = MinecraftPacketIds.NetworkSettings;
                return true;
            case "PlayerAuthInputPacket":
                result = MinecraftPacketIds.PlayerAuthInputPacket;
                return true;
            case "CreativeContent":
                result = MinecraftPacketIds.CreativeContent;
                return true;
            case "PlayerEnchantOptions":
                result = MinecraftPacketIds.PlayerEnchantOptions;
                return true;
            case "ItemStackRequest":
                result = MinecraftPacketIds.ItemStackRequest;
                return true;
            case "ItemStackResponse":
                result = MinecraftPacketIds.ItemStackResponse;
                return true;
            case "PlayerArmorDamage":
                result = MinecraftPacketIds.PlayerArmorDamage;
                return true;
            case "CodeBuilderPacket":
                result = MinecraftPacketIds.CodeBuilderPacket;
                return true;
            case "UpdatePlayerGameType":
                result = MinecraftPacketIds.UpdatePlayerGameType;
                return true;
            case "EmoteList":
                result = MinecraftPacketIds.EmoteList;
                return true;
            case "PositionTrackingDBServerBroadcast":
                result = MinecraftPacketIds.PositionTrackingDBServerBroadcast;
                return true;
            case "PositionTrackingDBClientRequest":
                result = MinecraftPacketIds.PositionTrackingDBClientRequest;
                return true;
            case "DebugInfoPacket":
                result = MinecraftPacketIds.DebugInfoPacket;
                return true;
            case "PacketViolationWarning":
                result = MinecraftPacketIds.PacketViolationWarning;
                return true;
            case "MotionPredictionHints":
                result = MinecraftPacketIds.MotionPredictionHints;
                return true;
            case "TriggerAnimation":
                result = MinecraftPacketIds.TriggerAnimation;
                return true;
            case "CameraShake":
                result = MinecraftPacketIds.CameraShake;
                return true;
            case "PlayerFogSetting":
                result = MinecraftPacketIds.PlayerFogSetting;
                return true;
            case "CorrectPlayerMovePredictionPacket":
                result = MinecraftPacketIds.CorrectPlayerMovePredictionPacket;
                return true;
            case "ItemRegistryPacket":
                result = MinecraftPacketIds.ItemRegistryPacket;
                return true;
            case "ClientBoundDebugRendererPacket":
                result = MinecraftPacketIds.ClientBoundDebugRendererPacket;
                return true;
            case "SyncActorProperty":
                result = MinecraftPacketIds.SyncActorProperty;
                return true;
            case "AddVolumeEntityPacket":
                result = MinecraftPacketIds.AddVolumeEntityPacket;
                return true;
            case "RemoveVolumeEntityPacket":
                result = MinecraftPacketIds.RemoveVolumeEntityPacket;
                return true;
            case "SimulationTypePacket":
                result = MinecraftPacketIds.SimulationTypePacket;
                return true;
            case "NpcDialoguePacket":
                result = MinecraftPacketIds.NpcDialoguePacket;
                return true;
            case "EduUriResourcePacket":
                result = MinecraftPacketIds.EduUriResourcePacket;
                return true;
            case "CreatePhotoPacket":
                result = MinecraftPacketIds.CreatePhotoPacket;
                return true;
            case "UpdateSubChunkBlocks":
                result = MinecraftPacketIds.UpdateSubChunkBlocks;
                return true;
            case "SubChunkPacket":
                result = MinecraftPacketIds.SubChunkPacket;
                return true;
            case "SubChunkRequestPacket":
                result = MinecraftPacketIds.SubChunkRequestPacket;
                return true;
            case "PlayerStartItemCooldown":
                result = MinecraftPacketIds.PlayerStartItemCooldown;
                return true;
            case "ScriptMessagePacket":
                result = MinecraftPacketIds.ScriptMessagePacket;
                return true;
            case "CodeBuilderSourcePacket":
                result = MinecraftPacketIds.CodeBuilderSourcePacket;
                return true;
            case "TickingAreasLoadStatus":
                result = MinecraftPacketIds.TickingAreasLoadStatus;
                return true;
            case "DimensionDataPacket":
                result = MinecraftPacketIds.DimensionDataPacket;
                return true;
            case "AgentAction":
                result = MinecraftPacketIds.AgentAction;
                return true;
            case "ChangeMobProperty":
                result = MinecraftPacketIds.ChangeMobProperty;
                return true;
            case "LessonProgressPacket":
                result = MinecraftPacketIds.LessonProgressPacket;
                return true;
            case "RequestAbilityPacket":
                result = MinecraftPacketIds.RequestAbilityPacket;
                return true;
            case "RequestPermissionsPacket":
                result = MinecraftPacketIds.RequestPermissionsPacket;
                return true;
            case "ToastRequest":
                result = MinecraftPacketIds.ToastRequest;
                return true;
            case "UpdateAbilitiesPacket":
                result = MinecraftPacketIds.UpdateAbilitiesPacket;
                return true;
            case "UpdateAdventureSettingsPacket":
                result = MinecraftPacketIds.UpdateAdventureSettingsPacket;
                return true;
            case "DeathInfo":
                result = MinecraftPacketIds.DeathInfo;
                return true;
            case "EditorNetworkPacket":
                result = MinecraftPacketIds.EditorNetworkPacket;
                return true;
            case "FeatureRegistryPacket":
                result = MinecraftPacketIds.FeatureRegistryPacket;
                return true;
            case "ServerStats":
                result = MinecraftPacketIds.ServerStats;
                return true;
            case "RequestNetworkSettings":
                result = MinecraftPacketIds.RequestNetworkSettings;
                return true;
            case "GameTestRequestPacket":
                result = MinecraftPacketIds.GameTestRequestPacket;
                return true;
            case "GameTestResultsPacket":
                result = MinecraftPacketIds.GameTestResultsPacket;
                return true;
            case "PlayerClientInputPermissions":
                result = MinecraftPacketIds.PlayerClientInputPermissions;
                return true;
            case "CameraPresets":
                result = MinecraftPacketIds.CameraPresets;
                return true;
            case "UnlockedRecipes":
                result = MinecraftPacketIds.UnlockedRecipes;
                return true;
            case "TitleSpecificPacketsStart":
                result = MinecraftPacketIds.TitleSpecificPacketsStart;
                return true;
            case "TitleSpecificPacketsEnd":
                result = MinecraftPacketIds.TitleSpecificPacketsEnd;
                return true;
            case "CameraInstruction":
                result = MinecraftPacketIds.CameraInstruction;
                return true;
            case "TrimData":
                result = MinecraftPacketIds.TrimData;
                return true;
            case "OpenSign":
                result = MinecraftPacketIds.OpenSign;
                return true;
            case "AgentAnimation":
                result = MinecraftPacketIds.AgentAnimation;
                return true;
            case "RefreshEntitlementsPacket":
                result = MinecraftPacketIds.RefreshEntitlementsPacket;
                return true;
            case "PlayerToggleCrafterSlotRequestPacket":
                result = MinecraftPacketIds.PlayerToggleCrafterSlotRequestPacket;
                return true;
            case "SetPlayerInventoryOptions":
                result = MinecraftPacketIds.SetPlayerInventoryOptions;
                return true;
            case "SetHudPacket":
                result = MinecraftPacketIds.SetHudPacket;
                return true;
            case "AwardAchievementPacket":
                result = MinecraftPacketIds.AwardAchievementPacket;
                return true;
            case "ClientboundCloseScreen":
                result = MinecraftPacketIds.ClientboundCloseScreen;
                return true;
            case "ServerboundLoadingScreenPacket":
                result = MinecraftPacketIds.ServerboundLoadingScreenPacket;
                return true;
            case "JigsawStructureDataPacket":
                result = MinecraftPacketIds.JigsawStructureDataPacket;
                return true;
            case "CurrentStructureFeaturePacket":
                result = MinecraftPacketIds.CurrentStructureFeaturePacket;
                return true;
            case "ServerboundDiagnosticsPacket":
                result = MinecraftPacketIds.ServerboundDiagnosticsPacket;
                return true;
            case "CameraAimAssist":
                result = MinecraftPacketIds.CameraAimAssist;
                return true;
            case "ContainerRegistryCleanup":
                result = MinecraftPacketIds.ContainerRegistryCleanup;
                return true;
            case "MovementEffect":
                result = MinecraftPacketIds.MovementEffect;
                return true;
            case "CameraAimAssistActorPriority":
                result = MinecraftPacketIds.CameraAimAssistActorPriority;
                return true;
            case "CameraAimAssistPresets":
                result = MinecraftPacketIds.CameraAimAssistPresets;
                return true;
            case "ClientCameraAimAssist":
                result = MinecraftPacketIds.ClientCameraAimAssist;
                return true;
            case "ClientMovementPredictionSyncPacket":
                result = MinecraftPacketIds.ClientMovementPredictionSyncPacket;
                return true;
            case "UpdateClientOptions":
                result = MinecraftPacketIds.UpdateClientOptions;
                return true;
            case "PlayerVideoCapturePacket":
                result = MinecraftPacketIds.PlayerVideoCapturePacket;
                return true;
            case "PlayerUpdateEntityOverridesPacket":
                result = MinecraftPacketIds.PlayerUpdateEntityOverridesPacket;
                return true;
            case "PlayerLocation":
                result = MinecraftPacketIds.PlayerLocation;
                return true;
            case "SyncWorldClocks":
                result = MinecraftPacketIds.SyncWorldClocks;
                return true;
            case "SendPartyDestinationCookie":
                result = MinecraftPacketIds.SendPartyDestinationCookie;
                return true;
            case "PartyDestinationCookieResponse":
                result = MinecraftPacketIds.PartyDestinationCookieResponse;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

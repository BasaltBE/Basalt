using System;

namespace BedrockProtocol.Enums;

public enum DisconnectFailReason {
    Unknown = 0,
    CantConnectNoInternet = 1,
    NoPermissions = 2,
    UnrecoverableError = 3,
    ThirdPartyBlocked = 4,
    ThirdPartyNoInternet = 5,
    ThirdPartyBadIP = 6,
    ThirdPartyNoServerOrServerLocked = 7,
    VersionMismatch = 8,
    SkinIssue = 9,
    InviteSessionNotFound = 10,
    EduLevelSettingsMissing = 11,
    LocalServerNotFound = 12,
    LegacyDisconnect = 13,
    INTERNAL_UserLeaveGameAttempted = 14,
    PlatformLockedSkinsError = 15,
    RealmsWorldUnassigned = 16,
    RealmsServerCantConnect = 17,
    RealmsServerHidden = 18,
    RealmsServerDisabledBeta = 19,
    RealmsServerDisabled = 20,
    CrossPlatformDisabled = 21,
    TESTONLY_CantConnect = 22,
    SessionNotFound = 23,
    ClientSettingsIncompatibleWithServer = 24,
    ServerFull = 25,
    InvalidPlatformSkin = 26,
    EditionVersionMismatch = 27,
    EditionMismatch = 28,
    LevelNewerThanExeVersion = 29,
    INTERNAL_NoFailOccurred = 30,
    BannedSkin = 31,
    Timeout = 32,
    ServerNotFound = 33,
    OutdatedServer = 34,
    OutdatedClient = 35,
    NoPremiumPlatform = 36,
    MultiplayerDisabled = 37,
    NoWiFi = 38,
    WorldCorruption = 39,
    NoReason = 40,
    Disconnected = 41,
    InvalidPlayer = 42,
    LoggedInOtherLocation = 43,
    ServerIdConflict = 44,
    NotAllowed = 45,
    NotAuthenticated = 46,
    InvalidTenant = 47,
    UnknownPacket = 48,
    UnexpectedPacket = 49,
    InvalidCommandRequestPacket = 50,
    HostSuspended = 51,
    LoginPacketNoRequest = 52,
    LoginPacketNoCert = 53,
    MissingClient = 54,
    Kicked = 55,
    KickedForExploit = 56,
    KickedForIdle = 57,
    ResourcePackProblem = 58,
    IncompatiblePack = 59,
    OutOfStorage = 60,
    InvalidLevel = 61,
    DisconnectPacket = 62,
    BlockMismatch = 63,
    InvalidHeights = 64,
    InvalidWidths = 65,
    ConnectionLost = 66,
    ZombieConnection = 67,
    Shutdown = 68,
    ReasonNotSet = 69,
    LoadingStateTimeout = 70,
    ResourcePackLoadingFailed = 71,
    SearchingForSessionLoadingScreenFailed = 72,
    NetherNetProtocolVersion = 73,
    SubsystemStatusError = 74,
    EmptyAuthFromDiscovery = 75,
    EmptyUrlFromDiscovery = 76,
    ExpiredAuthFromDiscovery = 77,
    UnknownSignalServiceSignInFailure = 78,
    XBLJoinLobbyFailure = 79,
    UnspecifiedClientInstanceDisconnection = 80,
    NetherNetSessionNotFound = 81,
    NetherNetCreatePeerConnection = 82,
    NetherNetICE = 83,
    NetherNetConnectRequest = 84,
    NetherNetConnectResponse = 85,
    NetherNetNegotiationTimeout = 86,
    NetherNetInactivityTimeout = 87,
    StaleConnectionBeingReplaced = 88,
    RealmsSessionNotFound = 89,
    BadPacket = 90,
    NetherNetFailedToCreateOffer = 91,
    NetherNetFailedToCreateAnswer = 92,
    NetherNetFailedToSetLocalDescription = 93,
    NetherNetFailedToSetRemoteDescription = 94,
    NetherNetNegotiationTimeoutWaitingForResponse = 95,
    NetherNetNegotiationTimeoutWaitingForAccept = 96,
    NetherNetIncomingConnectionIgnored = 97,
    NetherNetSignalingParsingFailure = 98,
    NetherNetSignalingUnknownError = 99,
    NetherNetSignalingUnicastDeliveryFailed = 100,
    NetherNetSignalingBroadcastDeliveryFailed = 101,
    NetherNetSignalingGenericDeliveryFailed = 102,
    EditorMismatchEditorWorld = 103,
    EditorMismatchVanillaWorld = 104,
    WorldTransferNotPrimaryClient = 105,
    INTERNAL_RequestServerShutdown = 106,
    ClientGameSetupCancelled = 107,
    ClientGameSetupFailed = 108,
    NoVenue = 109,
    NetherNetSignalingSigninFailed = 110,
    SessionAccessDenied = 111,
    ServiceSigninIssue = 112,
    NetherNetNoSignalingChannel = 113,
    NetherNetNotLoggedIn = 114,
    NetherNetClientSignalingError = 115,
    SubClientLoginDisabled = 116,
    DeepLinkTryingToOpenDemoWorldWhileSignedIn = 117,
    AsyncJoinTaskDenied = 118,
    RealmsTimelineRequired = 119,
    GuestWithoutHost = 120,
    FailedToJoinExperience = 121,
    NetherNetDataChannelClosed = 122,
    DiscoveryEnvironmentMismatch = 123,
    HostWithoutKeys = 124,
    HostSignedOut = 125,
    ScriptWatchdogException = 126,
    ScriptMemoryLimitExceeded = 127,
    StorageLowDuringGameplay = 128,
    StorageFullDuringGameplay = 129,
    LevelStorageCorruption = 130,
    EditionMismatchVanillaToEdu = 131,
    EditionMismatchEduToVanilla = 132,
    EditorMismatchEditorToVanilla = 133,
    EditorMismatchVanillaToEditor = 134,
    DenyListed = 135,
    NonceMissing = 136,
    NonceNotFound = 137,
    NonceExpired = 138,
    NonceNotValid = 139,
    HostDisconnected = 140,
    EditorJoinIntentPolicyFailure = 141,
    NetherNetIdentityNotAllowed = 142,
    InvalidName = 143,
    ExpiredToken = 144,
    HostAcceptsNoTypeOfAuth = 145,
    NotAuthenticatedFastFail = 146,
    EditorNotAllowed = 147,
}

public static class DisconnectFailReasonExtensions {
    public static string ToProtoString(this DisconnectFailReason value) => value.ToProtocolString();

    public static string ToProtocolString(this DisconnectFailReason value) {
        return value switch {
            DisconnectFailReason.Unknown => "Unknown",
            DisconnectFailReason.CantConnectNoInternet => "CantConnectNoInternet",
            DisconnectFailReason.NoPermissions => "NoPermissions",
            DisconnectFailReason.UnrecoverableError => "UnrecoverableError",
            DisconnectFailReason.ThirdPartyBlocked => "ThirdPartyBlocked",
            DisconnectFailReason.ThirdPartyNoInternet => "ThirdPartyNoInternet",
            DisconnectFailReason.ThirdPartyBadIP => "ThirdPartyBadIP",
            DisconnectFailReason.ThirdPartyNoServerOrServerLocked => "ThirdPartyNoServerOrServerLocked",
            DisconnectFailReason.VersionMismatch => "VersionMismatch",
            DisconnectFailReason.SkinIssue => "SkinIssue",
            DisconnectFailReason.InviteSessionNotFound => "InviteSessionNotFound",
            DisconnectFailReason.EduLevelSettingsMissing => "EduLevelSettingsMissing",
            DisconnectFailReason.LocalServerNotFound => "LocalServerNotFound",
            DisconnectFailReason.LegacyDisconnect => "LegacyDisconnect",
            DisconnectFailReason.INTERNAL_UserLeaveGameAttempted => "INTERNAL_UserLeaveGameAttempted",
            DisconnectFailReason.PlatformLockedSkinsError => "PlatformLockedSkinsError",
            DisconnectFailReason.RealmsWorldUnassigned => "RealmsWorldUnassigned",
            DisconnectFailReason.RealmsServerCantConnect => "RealmsServerCantConnect",
            DisconnectFailReason.RealmsServerHidden => "RealmsServerHidden",
            DisconnectFailReason.RealmsServerDisabledBeta => "RealmsServerDisabledBeta",
            DisconnectFailReason.RealmsServerDisabled => "RealmsServerDisabled",
            DisconnectFailReason.CrossPlatformDisabled => "CrossPlatformDisabled",
            DisconnectFailReason.TESTONLY_CantConnect => "TESTONLY_CantConnect",
            DisconnectFailReason.SessionNotFound => "SessionNotFound",
            DisconnectFailReason.ClientSettingsIncompatibleWithServer => "ClientSettingsIncompatibleWithServer",
            DisconnectFailReason.ServerFull => "ServerFull",
            DisconnectFailReason.InvalidPlatformSkin => "InvalidPlatformSkin",
            DisconnectFailReason.EditionVersionMismatch => "EditionVersionMismatch",
            DisconnectFailReason.EditionMismatch => "EditionMismatch",
            DisconnectFailReason.LevelNewerThanExeVersion => "LevelNewerThanExeVersion",
            DisconnectFailReason.INTERNAL_NoFailOccurred => "INTERNAL_NoFailOccurred",
            DisconnectFailReason.BannedSkin => "BannedSkin",
            DisconnectFailReason.Timeout => "Timeout",
            DisconnectFailReason.ServerNotFound => "ServerNotFound",
            DisconnectFailReason.OutdatedServer => "OutdatedServer",
            DisconnectFailReason.OutdatedClient => "OutdatedClient",
            DisconnectFailReason.NoPremiumPlatform => "NoPremiumPlatform",
            DisconnectFailReason.MultiplayerDisabled => "MultiplayerDisabled",
            DisconnectFailReason.NoWiFi => "NoWiFi",
            DisconnectFailReason.WorldCorruption => "WorldCorruption",
            DisconnectFailReason.NoReason => "NoReason",
            DisconnectFailReason.Disconnected => "Disconnected",
            DisconnectFailReason.InvalidPlayer => "InvalidPlayer",
            DisconnectFailReason.LoggedInOtherLocation => "LoggedInOtherLocation",
            DisconnectFailReason.ServerIdConflict => "ServerIdConflict",
            DisconnectFailReason.NotAllowed => "NotAllowed",
            DisconnectFailReason.NotAuthenticated => "NotAuthenticated",
            DisconnectFailReason.InvalidTenant => "InvalidTenant",
            DisconnectFailReason.UnknownPacket => "UnknownPacket",
            DisconnectFailReason.UnexpectedPacket => "UnexpectedPacket",
            DisconnectFailReason.InvalidCommandRequestPacket => "InvalidCommandRequestPacket",
            DisconnectFailReason.HostSuspended => "HostSuspended",
            DisconnectFailReason.LoginPacketNoRequest => "LoginPacketNoRequest",
            DisconnectFailReason.LoginPacketNoCert => "LoginPacketNoCert",
            DisconnectFailReason.MissingClient => "MissingClient",
            DisconnectFailReason.Kicked => "Kicked",
            DisconnectFailReason.KickedForExploit => "KickedForExploit",
            DisconnectFailReason.KickedForIdle => "KickedForIdle",
            DisconnectFailReason.ResourcePackProblem => "ResourcePackProblem",
            DisconnectFailReason.IncompatiblePack => "IncompatiblePack",
            DisconnectFailReason.OutOfStorage => "OutOfStorage",
            DisconnectFailReason.InvalidLevel => "InvalidLevel",
            DisconnectFailReason.DisconnectPacket => "DisconnectPacket",
            DisconnectFailReason.BlockMismatch => "BlockMismatch",
            DisconnectFailReason.InvalidHeights => "InvalidHeights",
            DisconnectFailReason.InvalidWidths => "InvalidWidths",
            DisconnectFailReason.ConnectionLost => "ConnectionLost",
            DisconnectFailReason.ZombieConnection => "ZombieConnection",
            DisconnectFailReason.Shutdown => "Shutdown",
            DisconnectFailReason.ReasonNotSet => "ReasonNotSet",
            DisconnectFailReason.LoadingStateTimeout => "LoadingStateTimeout",
            DisconnectFailReason.ResourcePackLoadingFailed => "ResourcePackLoadingFailed",
            DisconnectFailReason.SearchingForSessionLoadingScreenFailed => "SearchingForSessionLoadingScreenFailed",
            DisconnectFailReason.NetherNetProtocolVersion => "NetherNetProtocolVersion",
            DisconnectFailReason.SubsystemStatusError => "SubsystemStatusError",
            DisconnectFailReason.EmptyAuthFromDiscovery => "EmptyAuthFromDiscovery",
            DisconnectFailReason.EmptyUrlFromDiscovery => "EmptyUrlFromDiscovery",
            DisconnectFailReason.ExpiredAuthFromDiscovery => "ExpiredAuthFromDiscovery",
            DisconnectFailReason.UnknownSignalServiceSignInFailure => "UnknownSignalServiceSignInFailure",
            DisconnectFailReason.XBLJoinLobbyFailure => "XBLJoinLobbyFailure",
            DisconnectFailReason.UnspecifiedClientInstanceDisconnection => "UnspecifiedClientInstanceDisconnection",
            DisconnectFailReason.NetherNetSessionNotFound => "NetherNetSessionNotFound",
            DisconnectFailReason.NetherNetCreatePeerConnection => "NetherNetCreatePeerConnection",
            DisconnectFailReason.NetherNetICE => "NetherNetICE",
            DisconnectFailReason.NetherNetConnectRequest => "NetherNetConnectRequest",
            DisconnectFailReason.NetherNetConnectResponse => "NetherNetConnectResponse",
            DisconnectFailReason.NetherNetNegotiationTimeout => "NetherNetNegotiationTimeout",
            DisconnectFailReason.NetherNetInactivityTimeout => "NetherNetInactivityTimeout",
            DisconnectFailReason.StaleConnectionBeingReplaced => "StaleConnectionBeingReplaced",
            DisconnectFailReason.RealmsSessionNotFound => "RealmsSessionNotFound",
            DisconnectFailReason.BadPacket => "BadPacket",
            DisconnectFailReason.NetherNetFailedToCreateOffer => "NetherNetFailedToCreateOffer",
            DisconnectFailReason.NetherNetFailedToCreateAnswer => "NetherNetFailedToCreateAnswer",
            DisconnectFailReason.NetherNetFailedToSetLocalDescription => "NetherNetFailedToSetLocalDescription",
            DisconnectFailReason.NetherNetFailedToSetRemoteDescription => "NetherNetFailedToSetRemoteDescription",
            DisconnectFailReason.NetherNetNegotiationTimeoutWaitingForResponse => "NetherNetNegotiationTimeoutWaitingForResponse",
            DisconnectFailReason.NetherNetNegotiationTimeoutWaitingForAccept => "NetherNetNegotiationTimeoutWaitingForAccept",
            DisconnectFailReason.NetherNetIncomingConnectionIgnored => "NetherNetIncomingConnectionIgnored",
            DisconnectFailReason.NetherNetSignalingParsingFailure => "NetherNetSignalingParsingFailure",
            DisconnectFailReason.NetherNetSignalingUnknownError => "NetherNetSignalingUnknownError",
            DisconnectFailReason.NetherNetSignalingUnicastDeliveryFailed => "NetherNetSignalingUnicastDeliveryFailed",
            DisconnectFailReason.NetherNetSignalingBroadcastDeliveryFailed => "NetherNetSignalingBroadcastDeliveryFailed",
            DisconnectFailReason.NetherNetSignalingGenericDeliveryFailed => "NetherNetSignalingGenericDeliveryFailed",
            DisconnectFailReason.EditorMismatchEditorWorld => "EditorMismatchEditorWorld",
            DisconnectFailReason.EditorMismatchVanillaWorld => "EditorMismatchVanillaWorld",
            DisconnectFailReason.WorldTransferNotPrimaryClient => "WorldTransferNotPrimaryClient",
            DisconnectFailReason.INTERNAL_RequestServerShutdown => "INTERNAL_RequestServerShutdown",
            DisconnectFailReason.ClientGameSetupCancelled => "ClientGameSetupCancelled",
            DisconnectFailReason.ClientGameSetupFailed => "ClientGameSetupFailed",
            DisconnectFailReason.NoVenue => "NoVenue",
            DisconnectFailReason.NetherNetSignalingSigninFailed => "NetherNetSignalingSigninFailed",
            DisconnectFailReason.SessionAccessDenied => "SessionAccessDenied",
            DisconnectFailReason.ServiceSigninIssue => "ServiceSigninIssue",
            DisconnectFailReason.NetherNetNoSignalingChannel => "NetherNetNoSignalingChannel",
            DisconnectFailReason.NetherNetNotLoggedIn => "NetherNetNotLoggedIn",
            DisconnectFailReason.NetherNetClientSignalingError => "NetherNetClientSignalingError",
            DisconnectFailReason.SubClientLoginDisabled => "SubClientLoginDisabled",
            DisconnectFailReason.DeepLinkTryingToOpenDemoWorldWhileSignedIn => "DeepLinkTryingToOpenDemoWorldWhileSignedIn",
            DisconnectFailReason.AsyncJoinTaskDenied => "AsyncJoinTaskDenied",
            DisconnectFailReason.RealmsTimelineRequired => "RealmsTimelineRequired",
            DisconnectFailReason.GuestWithoutHost => "GuestWithoutHost",
            DisconnectFailReason.FailedToJoinExperience => "FailedToJoinExperience",
            DisconnectFailReason.NetherNetDataChannelClosed => "NetherNetDataChannelClosed",
            DisconnectFailReason.DiscoveryEnvironmentMismatch => "DiscoveryEnvironmentMismatch",
            DisconnectFailReason.HostWithoutKeys => "HostWithoutKeys",
            DisconnectFailReason.HostSignedOut => "HostSignedOut",
            DisconnectFailReason.ScriptWatchdogException => "ScriptWatchdogException",
            DisconnectFailReason.ScriptMemoryLimitExceeded => "ScriptMemoryLimitExceeded",
            DisconnectFailReason.StorageLowDuringGameplay => "StorageLowDuringGameplay",
            DisconnectFailReason.StorageFullDuringGameplay => "StorageFullDuringGameplay",
            DisconnectFailReason.LevelStorageCorruption => "LevelStorageCorruption",
            DisconnectFailReason.EditionMismatchVanillaToEdu => "EditionMismatchVanillaToEdu",
            DisconnectFailReason.EditionMismatchEduToVanilla => "EditionMismatchEduToVanilla",
            DisconnectFailReason.EditorMismatchEditorToVanilla => "EditorMismatchEditorToVanilla",
            DisconnectFailReason.EditorMismatchVanillaToEditor => "EditorMismatchVanillaToEditor",
            DisconnectFailReason.DenyListed => "DenyListed",
            DisconnectFailReason.NonceMissing => "NonceMissing",
            DisconnectFailReason.NonceNotFound => "NonceNotFound",
            DisconnectFailReason.NonceExpired => "NonceExpired",
            DisconnectFailReason.NonceNotValid => "NonceNotValid",
            DisconnectFailReason.HostDisconnected => "HostDisconnected",
            DisconnectFailReason.EditorJoinIntentPolicyFailure => "EditorJoinIntentPolicyFailure",
            DisconnectFailReason.NetherNetIdentityNotAllowed => "NetherNetIdentityNotAllowed",
            DisconnectFailReason.InvalidName => "InvalidName",
            DisconnectFailReason.ExpiredToken => "ExpiredToken",
            DisconnectFailReason.HostAcceptsNoTypeOfAuth => "HostAcceptsNoTypeOfAuth",
            DisconnectFailReason.NotAuthenticatedFastFail => "NotAuthenticatedFastFail",
            DisconnectFailReason.EditorNotAllowed => "EditorNotAllowed",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown DisconnectFailReason value.")
        };
    }

    public static DisconnectFailReason FromProtocolString(string value) {
        return value switch {
            "Unknown" => DisconnectFailReason.Unknown,
            "CantConnectNoInternet" => DisconnectFailReason.CantConnectNoInternet,
            "NoPermissions" => DisconnectFailReason.NoPermissions,
            "UnrecoverableError" => DisconnectFailReason.UnrecoverableError,
            "ThirdPartyBlocked" => DisconnectFailReason.ThirdPartyBlocked,
            "ThirdPartyNoInternet" => DisconnectFailReason.ThirdPartyNoInternet,
            "ThirdPartyBadIP" => DisconnectFailReason.ThirdPartyBadIP,
            "ThirdPartyNoServerOrServerLocked" => DisconnectFailReason.ThirdPartyNoServerOrServerLocked,
            "VersionMismatch" => DisconnectFailReason.VersionMismatch,
            "SkinIssue" => DisconnectFailReason.SkinIssue,
            "InviteSessionNotFound" => DisconnectFailReason.InviteSessionNotFound,
            "EduLevelSettingsMissing" => DisconnectFailReason.EduLevelSettingsMissing,
            "LocalServerNotFound" => DisconnectFailReason.LocalServerNotFound,
            "LegacyDisconnect" => DisconnectFailReason.LegacyDisconnect,
            "INTERNAL_UserLeaveGameAttempted" => DisconnectFailReason.INTERNAL_UserLeaveGameAttempted,
            "PlatformLockedSkinsError" => DisconnectFailReason.PlatformLockedSkinsError,
            "RealmsWorldUnassigned" => DisconnectFailReason.RealmsWorldUnassigned,
            "RealmsServerCantConnect" => DisconnectFailReason.RealmsServerCantConnect,
            "RealmsServerHidden" => DisconnectFailReason.RealmsServerHidden,
            "RealmsServerDisabledBeta" => DisconnectFailReason.RealmsServerDisabledBeta,
            "RealmsServerDisabled" => DisconnectFailReason.RealmsServerDisabled,
            "CrossPlatformDisabled" => DisconnectFailReason.CrossPlatformDisabled,
            "TESTONLY_CantConnect" => DisconnectFailReason.TESTONLY_CantConnect,
            "SessionNotFound" => DisconnectFailReason.SessionNotFound,
            "ClientSettingsIncompatibleWithServer" => DisconnectFailReason.ClientSettingsIncompatibleWithServer,
            "ServerFull" => DisconnectFailReason.ServerFull,
            "InvalidPlatformSkin" => DisconnectFailReason.InvalidPlatformSkin,
            "EditionVersionMismatch" => DisconnectFailReason.EditionVersionMismatch,
            "EditionMismatch" => DisconnectFailReason.EditionMismatch,
            "LevelNewerThanExeVersion" => DisconnectFailReason.LevelNewerThanExeVersion,
            "INTERNAL_NoFailOccurred" => DisconnectFailReason.INTERNAL_NoFailOccurred,
            "BannedSkin" => DisconnectFailReason.BannedSkin,
            "Timeout" => DisconnectFailReason.Timeout,
            "ServerNotFound" => DisconnectFailReason.ServerNotFound,
            "OutdatedServer" => DisconnectFailReason.OutdatedServer,
            "OutdatedClient" => DisconnectFailReason.OutdatedClient,
            "NoPremiumPlatform" => DisconnectFailReason.NoPremiumPlatform,
            "MultiplayerDisabled" => DisconnectFailReason.MultiplayerDisabled,
            "NoWiFi" => DisconnectFailReason.NoWiFi,
            "WorldCorruption" => DisconnectFailReason.WorldCorruption,
            "NoReason" => DisconnectFailReason.NoReason,
            "Disconnected" => DisconnectFailReason.Disconnected,
            "InvalidPlayer" => DisconnectFailReason.InvalidPlayer,
            "LoggedInOtherLocation" => DisconnectFailReason.LoggedInOtherLocation,
            "ServerIdConflict" => DisconnectFailReason.ServerIdConflict,
            "NotAllowed" => DisconnectFailReason.NotAllowed,
            "NotAuthenticated" => DisconnectFailReason.NotAuthenticated,
            "InvalidTenant" => DisconnectFailReason.InvalidTenant,
            "UnknownPacket" => DisconnectFailReason.UnknownPacket,
            "UnexpectedPacket" => DisconnectFailReason.UnexpectedPacket,
            "InvalidCommandRequestPacket" => DisconnectFailReason.InvalidCommandRequestPacket,
            "HostSuspended" => DisconnectFailReason.HostSuspended,
            "LoginPacketNoRequest" => DisconnectFailReason.LoginPacketNoRequest,
            "LoginPacketNoCert" => DisconnectFailReason.LoginPacketNoCert,
            "MissingClient" => DisconnectFailReason.MissingClient,
            "Kicked" => DisconnectFailReason.Kicked,
            "KickedForExploit" => DisconnectFailReason.KickedForExploit,
            "KickedForIdle" => DisconnectFailReason.KickedForIdle,
            "ResourcePackProblem" => DisconnectFailReason.ResourcePackProblem,
            "IncompatiblePack" => DisconnectFailReason.IncompatiblePack,
            "OutOfStorage" => DisconnectFailReason.OutOfStorage,
            "InvalidLevel" => DisconnectFailReason.InvalidLevel,
            "DisconnectPacket" => DisconnectFailReason.DisconnectPacket,
            "BlockMismatch" => DisconnectFailReason.BlockMismatch,
            "InvalidHeights" => DisconnectFailReason.InvalidHeights,
            "InvalidWidths" => DisconnectFailReason.InvalidWidths,
            "ConnectionLost" => DisconnectFailReason.ConnectionLost,
            "ZombieConnection" => DisconnectFailReason.ZombieConnection,
            "Shutdown" => DisconnectFailReason.Shutdown,
            "ReasonNotSet" => DisconnectFailReason.ReasonNotSet,
            "LoadingStateTimeout" => DisconnectFailReason.LoadingStateTimeout,
            "ResourcePackLoadingFailed" => DisconnectFailReason.ResourcePackLoadingFailed,
            "SearchingForSessionLoadingScreenFailed" => DisconnectFailReason.SearchingForSessionLoadingScreenFailed,
            "NetherNetProtocolVersion" => DisconnectFailReason.NetherNetProtocolVersion,
            "SubsystemStatusError" => DisconnectFailReason.SubsystemStatusError,
            "EmptyAuthFromDiscovery" => DisconnectFailReason.EmptyAuthFromDiscovery,
            "EmptyUrlFromDiscovery" => DisconnectFailReason.EmptyUrlFromDiscovery,
            "ExpiredAuthFromDiscovery" => DisconnectFailReason.ExpiredAuthFromDiscovery,
            "UnknownSignalServiceSignInFailure" => DisconnectFailReason.UnknownSignalServiceSignInFailure,
            "XBLJoinLobbyFailure" => DisconnectFailReason.XBLJoinLobbyFailure,
            "UnspecifiedClientInstanceDisconnection" => DisconnectFailReason.UnspecifiedClientInstanceDisconnection,
            "NetherNetSessionNotFound" => DisconnectFailReason.NetherNetSessionNotFound,
            "NetherNetCreatePeerConnection" => DisconnectFailReason.NetherNetCreatePeerConnection,
            "NetherNetICE" => DisconnectFailReason.NetherNetICE,
            "NetherNetConnectRequest" => DisconnectFailReason.NetherNetConnectRequest,
            "NetherNetConnectResponse" => DisconnectFailReason.NetherNetConnectResponse,
            "NetherNetNegotiationTimeout" => DisconnectFailReason.NetherNetNegotiationTimeout,
            "NetherNetInactivityTimeout" => DisconnectFailReason.NetherNetInactivityTimeout,
            "StaleConnectionBeingReplaced" => DisconnectFailReason.StaleConnectionBeingReplaced,
            "RealmsSessionNotFound" => DisconnectFailReason.RealmsSessionNotFound,
            "BadPacket" => DisconnectFailReason.BadPacket,
            "NetherNetFailedToCreateOffer" => DisconnectFailReason.NetherNetFailedToCreateOffer,
            "NetherNetFailedToCreateAnswer" => DisconnectFailReason.NetherNetFailedToCreateAnswer,
            "NetherNetFailedToSetLocalDescription" => DisconnectFailReason.NetherNetFailedToSetLocalDescription,
            "NetherNetFailedToSetRemoteDescription" => DisconnectFailReason.NetherNetFailedToSetRemoteDescription,
            "NetherNetNegotiationTimeoutWaitingForResponse" => DisconnectFailReason.NetherNetNegotiationTimeoutWaitingForResponse,
            "NetherNetNegotiationTimeoutWaitingForAccept" => DisconnectFailReason.NetherNetNegotiationTimeoutWaitingForAccept,
            "NetherNetIncomingConnectionIgnored" => DisconnectFailReason.NetherNetIncomingConnectionIgnored,
            "NetherNetSignalingParsingFailure" => DisconnectFailReason.NetherNetSignalingParsingFailure,
            "NetherNetSignalingUnknownError" => DisconnectFailReason.NetherNetSignalingUnknownError,
            "NetherNetSignalingUnicastDeliveryFailed" => DisconnectFailReason.NetherNetSignalingUnicastDeliveryFailed,
            "NetherNetSignalingBroadcastDeliveryFailed" => DisconnectFailReason.NetherNetSignalingBroadcastDeliveryFailed,
            "NetherNetSignalingGenericDeliveryFailed" => DisconnectFailReason.NetherNetSignalingGenericDeliveryFailed,
            "EditorMismatchEditorWorld" => DisconnectFailReason.EditorMismatchEditorWorld,
            "EditorMismatchVanillaWorld" => DisconnectFailReason.EditorMismatchVanillaWorld,
            "WorldTransferNotPrimaryClient" => DisconnectFailReason.WorldTransferNotPrimaryClient,
            "INTERNAL_RequestServerShutdown" => DisconnectFailReason.INTERNAL_RequestServerShutdown,
            "ClientGameSetupCancelled" => DisconnectFailReason.ClientGameSetupCancelled,
            "ClientGameSetupFailed" => DisconnectFailReason.ClientGameSetupFailed,
            "NoVenue" => DisconnectFailReason.NoVenue,
            "NetherNetSignalingSigninFailed" => DisconnectFailReason.NetherNetSignalingSigninFailed,
            "SessionAccessDenied" => DisconnectFailReason.SessionAccessDenied,
            "ServiceSigninIssue" => DisconnectFailReason.ServiceSigninIssue,
            "NetherNetNoSignalingChannel" => DisconnectFailReason.NetherNetNoSignalingChannel,
            "NetherNetNotLoggedIn" => DisconnectFailReason.NetherNetNotLoggedIn,
            "NetherNetClientSignalingError" => DisconnectFailReason.NetherNetClientSignalingError,
            "SubClientLoginDisabled" => DisconnectFailReason.SubClientLoginDisabled,
            "DeepLinkTryingToOpenDemoWorldWhileSignedIn" => DisconnectFailReason.DeepLinkTryingToOpenDemoWorldWhileSignedIn,
            "AsyncJoinTaskDenied" => DisconnectFailReason.AsyncJoinTaskDenied,
            "RealmsTimelineRequired" => DisconnectFailReason.RealmsTimelineRequired,
            "GuestWithoutHost" => DisconnectFailReason.GuestWithoutHost,
            "FailedToJoinExperience" => DisconnectFailReason.FailedToJoinExperience,
            "NetherNetDataChannelClosed" => DisconnectFailReason.NetherNetDataChannelClosed,
            "DiscoveryEnvironmentMismatch" => DisconnectFailReason.DiscoveryEnvironmentMismatch,
            "HostWithoutKeys" => DisconnectFailReason.HostWithoutKeys,
            "HostSignedOut" => DisconnectFailReason.HostSignedOut,
            "ScriptWatchdogException" => DisconnectFailReason.ScriptWatchdogException,
            "ScriptMemoryLimitExceeded" => DisconnectFailReason.ScriptMemoryLimitExceeded,
            "StorageLowDuringGameplay" => DisconnectFailReason.StorageLowDuringGameplay,
            "StorageFullDuringGameplay" => DisconnectFailReason.StorageFullDuringGameplay,
            "LevelStorageCorruption" => DisconnectFailReason.LevelStorageCorruption,
            "EditionMismatchVanillaToEdu" => DisconnectFailReason.EditionMismatchVanillaToEdu,
            "EditionMismatchEduToVanilla" => DisconnectFailReason.EditionMismatchEduToVanilla,
            "EditorMismatchEditorToVanilla" => DisconnectFailReason.EditorMismatchEditorToVanilla,
            "EditorMismatchVanillaToEditor" => DisconnectFailReason.EditorMismatchVanillaToEditor,
            "DenyListed" => DisconnectFailReason.DenyListed,
            "NonceMissing" => DisconnectFailReason.NonceMissing,
            "NonceNotFound" => DisconnectFailReason.NonceNotFound,
            "NonceExpired" => DisconnectFailReason.NonceExpired,
            "NonceNotValid" => DisconnectFailReason.NonceNotValid,
            "HostDisconnected" => DisconnectFailReason.HostDisconnected,
            "EditorJoinIntentPolicyFailure" => DisconnectFailReason.EditorJoinIntentPolicyFailure,
            "NetherNetIdentityNotAllowed" => DisconnectFailReason.NetherNetIdentityNotAllowed,
            "InvalidName" => DisconnectFailReason.InvalidName,
            "ExpiredToken" => DisconnectFailReason.ExpiredToken,
            "HostAcceptsNoTypeOfAuth" => DisconnectFailReason.HostAcceptsNoTypeOfAuth,
            "NotAuthenticatedFastFail" => DisconnectFailReason.NotAuthenticatedFastFail,
            "EditorNotAllowed" => DisconnectFailReason.EditorNotAllowed,
            _ => throw new ArgumentException($"Unknown DisconnectFailReason protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out DisconnectFailReason result) {
        switch (value) {
            case "Unknown":
                result = DisconnectFailReason.Unknown;
                return true;
            case "CantConnectNoInternet":
                result = DisconnectFailReason.CantConnectNoInternet;
                return true;
            case "NoPermissions":
                result = DisconnectFailReason.NoPermissions;
                return true;
            case "UnrecoverableError":
                result = DisconnectFailReason.UnrecoverableError;
                return true;
            case "ThirdPartyBlocked":
                result = DisconnectFailReason.ThirdPartyBlocked;
                return true;
            case "ThirdPartyNoInternet":
                result = DisconnectFailReason.ThirdPartyNoInternet;
                return true;
            case "ThirdPartyBadIP":
                result = DisconnectFailReason.ThirdPartyBadIP;
                return true;
            case "ThirdPartyNoServerOrServerLocked":
                result = DisconnectFailReason.ThirdPartyNoServerOrServerLocked;
                return true;
            case "VersionMismatch":
                result = DisconnectFailReason.VersionMismatch;
                return true;
            case "SkinIssue":
                result = DisconnectFailReason.SkinIssue;
                return true;
            case "InviteSessionNotFound":
                result = DisconnectFailReason.InviteSessionNotFound;
                return true;
            case "EduLevelSettingsMissing":
                result = DisconnectFailReason.EduLevelSettingsMissing;
                return true;
            case "LocalServerNotFound":
                result = DisconnectFailReason.LocalServerNotFound;
                return true;
            case "LegacyDisconnect":
                result = DisconnectFailReason.LegacyDisconnect;
                return true;
            case "INTERNAL_UserLeaveGameAttempted":
                result = DisconnectFailReason.INTERNAL_UserLeaveGameAttempted;
                return true;
            case "PlatformLockedSkinsError":
                result = DisconnectFailReason.PlatformLockedSkinsError;
                return true;
            case "RealmsWorldUnassigned":
                result = DisconnectFailReason.RealmsWorldUnassigned;
                return true;
            case "RealmsServerCantConnect":
                result = DisconnectFailReason.RealmsServerCantConnect;
                return true;
            case "RealmsServerHidden":
                result = DisconnectFailReason.RealmsServerHidden;
                return true;
            case "RealmsServerDisabledBeta":
                result = DisconnectFailReason.RealmsServerDisabledBeta;
                return true;
            case "RealmsServerDisabled":
                result = DisconnectFailReason.RealmsServerDisabled;
                return true;
            case "CrossPlatformDisabled":
                result = DisconnectFailReason.CrossPlatformDisabled;
                return true;
            case "TESTONLY_CantConnect":
                result = DisconnectFailReason.TESTONLY_CantConnect;
                return true;
            case "SessionNotFound":
                result = DisconnectFailReason.SessionNotFound;
                return true;
            case "ClientSettingsIncompatibleWithServer":
                result = DisconnectFailReason.ClientSettingsIncompatibleWithServer;
                return true;
            case "ServerFull":
                result = DisconnectFailReason.ServerFull;
                return true;
            case "InvalidPlatformSkin":
                result = DisconnectFailReason.InvalidPlatformSkin;
                return true;
            case "EditionVersionMismatch":
                result = DisconnectFailReason.EditionVersionMismatch;
                return true;
            case "EditionMismatch":
                result = DisconnectFailReason.EditionMismatch;
                return true;
            case "LevelNewerThanExeVersion":
                result = DisconnectFailReason.LevelNewerThanExeVersion;
                return true;
            case "INTERNAL_NoFailOccurred":
                result = DisconnectFailReason.INTERNAL_NoFailOccurred;
                return true;
            case "BannedSkin":
                result = DisconnectFailReason.BannedSkin;
                return true;
            case "Timeout":
                result = DisconnectFailReason.Timeout;
                return true;
            case "ServerNotFound":
                result = DisconnectFailReason.ServerNotFound;
                return true;
            case "OutdatedServer":
                result = DisconnectFailReason.OutdatedServer;
                return true;
            case "OutdatedClient":
                result = DisconnectFailReason.OutdatedClient;
                return true;
            case "NoPremiumPlatform":
                result = DisconnectFailReason.NoPremiumPlatform;
                return true;
            case "MultiplayerDisabled":
                result = DisconnectFailReason.MultiplayerDisabled;
                return true;
            case "NoWiFi":
                result = DisconnectFailReason.NoWiFi;
                return true;
            case "WorldCorruption":
                result = DisconnectFailReason.WorldCorruption;
                return true;
            case "NoReason":
                result = DisconnectFailReason.NoReason;
                return true;
            case "Disconnected":
                result = DisconnectFailReason.Disconnected;
                return true;
            case "InvalidPlayer":
                result = DisconnectFailReason.InvalidPlayer;
                return true;
            case "LoggedInOtherLocation":
                result = DisconnectFailReason.LoggedInOtherLocation;
                return true;
            case "ServerIdConflict":
                result = DisconnectFailReason.ServerIdConflict;
                return true;
            case "NotAllowed":
                result = DisconnectFailReason.NotAllowed;
                return true;
            case "NotAuthenticated":
                result = DisconnectFailReason.NotAuthenticated;
                return true;
            case "InvalidTenant":
                result = DisconnectFailReason.InvalidTenant;
                return true;
            case "UnknownPacket":
                result = DisconnectFailReason.UnknownPacket;
                return true;
            case "UnexpectedPacket":
                result = DisconnectFailReason.UnexpectedPacket;
                return true;
            case "InvalidCommandRequestPacket":
                result = DisconnectFailReason.InvalidCommandRequestPacket;
                return true;
            case "HostSuspended":
                result = DisconnectFailReason.HostSuspended;
                return true;
            case "LoginPacketNoRequest":
                result = DisconnectFailReason.LoginPacketNoRequest;
                return true;
            case "LoginPacketNoCert":
                result = DisconnectFailReason.LoginPacketNoCert;
                return true;
            case "MissingClient":
                result = DisconnectFailReason.MissingClient;
                return true;
            case "Kicked":
                result = DisconnectFailReason.Kicked;
                return true;
            case "KickedForExploit":
                result = DisconnectFailReason.KickedForExploit;
                return true;
            case "KickedForIdle":
                result = DisconnectFailReason.KickedForIdle;
                return true;
            case "ResourcePackProblem":
                result = DisconnectFailReason.ResourcePackProblem;
                return true;
            case "IncompatiblePack":
                result = DisconnectFailReason.IncompatiblePack;
                return true;
            case "OutOfStorage":
                result = DisconnectFailReason.OutOfStorage;
                return true;
            case "InvalidLevel":
                result = DisconnectFailReason.InvalidLevel;
                return true;
            case "DisconnectPacket":
                result = DisconnectFailReason.DisconnectPacket;
                return true;
            case "BlockMismatch":
                result = DisconnectFailReason.BlockMismatch;
                return true;
            case "InvalidHeights":
                result = DisconnectFailReason.InvalidHeights;
                return true;
            case "InvalidWidths":
                result = DisconnectFailReason.InvalidWidths;
                return true;
            case "ConnectionLost":
                result = DisconnectFailReason.ConnectionLost;
                return true;
            case "ZombieConnection":
                result = DisconnectFailReason.ZombieConnection;
                return true;
            case "Shutdown":
                result = DisconnectFailReason.Shutdown;
                return true;
            case "ReasonNotSet":
                result = DisconnectFailReason.ReasonNotSet;
                return true;
            case "LoadingStateTimeout":
                result = DisconnectFailReason.LoadingStateTimeout;
                return true;
            case "ResourcePackLoadingFailed":
                result = DisconnectFailReason.ResourcePackLoadingFailed;
                return true;
            case "SearchingForSessionLoadingScreenFailed":
                result = DisconnectFailReason.SearchingForSessionLoadingScreenFailed;
                return true;
            case "NetherNetProtocolVersion":
                result = DisconnectFailReason.NetherNetProtocolVersion;
                return true;
            case "SubsystemStatusError":
                result = DisconnectFailReason.SubsystemStatusError;
                return true;
            case "EmptyAuthFromDiscovery":
                result = DisconnectFailReason.EmptyAuthFromDiscovery;
                return true;
            case "EmptyUrlFromDiscovery":
                result = DisconnectFailReason.EmptyUrlFromDiscovery;
                return true;
            case "ExpiredAuthFromDiscovery":
                result = DisconnectFailReason.ExpiredAuthFromDiscovery;
                return true;
            case "UnknownSignalServiceSignInFailure":
                result = DisconnectFailReason.UnknownSignalServiceSignInFailure;
                return true;
            case "XBLJoinLobbyFailure":
                result = DisconnectFailReason.XBLJoinLobbyFailure;
                return true;
            case "UnspecifiedClientInstanceDisconnection":
                result = DisconnectFailReason.UnspecifiedClientInstanceDisconnection;
                return true;
            case "NetherNetSessionNotFound":
                result = DisconnectFailReason.NetherNetSessionNotFound;
                return true;
            case "NetherNetCreatePeerConnection":
                result = DisconnectFailReason.NetherNetCreatePeerConnection;
                return true;
            case "NetherNetICE":
                result = DisconnectFailReason.NetherNetICE;
                return true;
            case "NetherNetConnectRequest":
                result = DisconnectFailReason.NetherNetConnectRequest;
                return true;
            case "NetherNetConnectResponse":
                result = DisconnectFailReason.NetherNetConnectResponse;
                return true;
            case "NetherNetNegotiationTimeout":
                result = DisconnectFailReason.NetherNetNegotiationTimeout;
                return true;
            case "NetherNetInactivityTimeout":
                result = DisconnectFailReason.NetherNetInactivityTimeout;
                return true;
            case "StaleConnectionBeingReplaced":
                result = DisconnectFailReason.StaleConnectionBeingReplaced;
                return true;
            case "RealmsSessionNotFound":
                result = DisconnectFailReason.RealmsSessionNotFound;
                return true;
            case "BadPacket":
                result = DisconnectFailReason.BadPacket;
                return true;
            case "NetherNetFailedToCreateOffer":
                result = DisconnectFailReason.NetherNetFailedToCreateOffer;
                return true;
            case "NetherNetFailedToCreateAnswer":
                result = DisconnectFailReason.NetherNetFailedToCreateAnswer;
                return true;
            case "NetherNetFailedToSetLocalDescription":
                result = DisconnectFailReason.NetherNetFailedToSetLocalDescription;
                return true;
            case "NetherNetFailedToSetRemoteDescription":
                result = DisconnectFailReason.NetherNetFailedToSetRemoteDescription;
                return true;
            case "NetherNetNegotiationTimeoutWaitingForResponse":
                result = DisconnectFailReason.NetherNetNegotiationTimeoutWaitingForResponse;
                return true;
            case "NetherNetNegotiationTimeoutWaitingForAccept":
                result = DisconnectFailReason.NetherNetNegotiationTimeoutWaitingForAccept;
                return true;
            case "NetherNetIncomingConnectionIgnored":
                result = DisconnectFailReason.NetherNetIncomingConnectionIgnored;
                return true;
            case "NetherNetSignalingParsingFailure":
                result = DisconnectFailReason.NetherNetSignalingParsingFailure;
                return true;
            case "NetherNetSignalingUnknownError":
                result = DisconnectFailReason.NetherNetSignalingUnknownError;
                return true;
            case "NetherNetSignalingUnicastDeliveryFailed":
                result = DisconnectFailReason.NetherNetSignalingUnicastDeliveryFailed;
                return true;
            case "NetherNetSignalingBroadcastDeliveryFailed":
                result = DisconnectFailReason.NetherNetSignalingBroadcastDeliveryFailed;
                return true;
            case "NetherNetSignalingGenericDeliveryFailed":
                result = DisconnectFailReason.NetherNetSignalingGenericDeliveryFailed;
                return true;
            case "EditorMismatchEditorWorld":
                result = DisconnectFailReason.EditorMismatchEditorWorld;
                return true;
            case "EditorMismatchVanillaWorld":
                result = DisconnectFailReason.EditorMismatchVanillaWorld;
                return true;
            case "WorldTransferNotPrimaryClient":
                result = DisconnectFailReason.WorldTransferNotPrimaryClient;
                return true;
            case "INTERNAL_RequestServerShutdown":
                result = DisconnectFailReason.INTERNAL_RequestServerShutdown;
                return true;
            case "ClientGameSetupCancelled":
                result = DisconnectFailReason.ClientGameSetupCancelled;
                return true;
            case "ClientGameSetupFailed":
                result = DisconnectFailReason.ClientGameSetupFailed;
                return true;
            case "NoVenue":
                result = DisconnectFailReason.NoVenue;
                return true;
            case "NetherNetSignalingSigninFailed":
                result = DisconnectFailReason.NetherNetSignalingSigninFailed;
                return true;
            case "SessionAccessDenied":
                result = DisconnectFailReason.SessionAccessDenied;
                return true;
            case "ServiceSigninIssue":
                result = DisconnectFailReason.ServiceSigninIssue;
                return true;
            case "NetherNetNoSignalingChannel":
                result = DisconnectFailReason.NetherNetNoSignalingChannel;
                return true;
            case "NetherNetNotLoggedIn":
                result = DisconnectFailReason.NetherNetNotLoggedIn;
                return true;
            case "NetherNetClientSignalingError":
                result = DisconnectFailReason.NetherNetClientSignalingError;
                return true;
            case "SubClientLoginDisabled":
                result = DisconnectFailReason.SubClientLoginDisabled;
                return true;
            case "DeepLinkTryingToOpenDemoWorldWhileSignedIn":
                result = DisconnectFailReason.DeepLinkTryingToOpenDemoWorldWhileSignedIn;
                return true;
            case "AsyncJoinTaskDenied":
                result = DisconnectFailReason.AsyncJoinTaskDenied;
                return true;
            case "RealmsTimelineRequired":
                result = DisconnectFailReason.RealmsTimelineRequired;
                return true;
            case "GuestWithoutHost":
                result = DisconnectFailReason.GuestWithoutHost;
                return true;
            case "FailedToJoinExperience":
                result = DisconnectFailReason.FailedToJoinExperience;
                return true;
            case "NetherNetDataChannelClosed":
                result = DisconnectFailReason.NetherNetDataChannelClosed;
                return true;
            case "DiscoveryEnvironmentMismatch":
                result = DisconnectFailReason.DiscoveryEnvironmentMismatch;
                return true;
            case "HostWithoutKeys":
                result = DisconnectFailReason.HostWithoutKeys;
                return true;
            case "HostSignedOut":
                result = DisconnectFailReason.HostSignedOut;
                return true;
            case "ScriptWatchdogException":
                result = DisconnectFailReason.ScriptWatchdogException;
                return true;
            case "ScriptMemoryLimitExceeded":
                result = DisconnectFailReason.ScriptMemoryLimitExceeded;
                return true;
            case "StorageLowDuringGameplay":
                result = DisconnectFailReason.StorageLowDuringGameplay;
                return true;
            case "StorageFullDuringGameplay":
                result = DisconnectFailReason.StorageFullDuringGameplay;
                return true;
            case "LevelStorageCorruption":
                result = DisconnectFailReason.LevelStorageCorruption;
                return true;
            case "EditionMismatchVanillaToEdu":
                result = DisconnectFailReason.EditionMismatchVanillaToEdu;
                return true;
            case "EditionMismatchEduToVanilla":
                result = DisconnectFailReason.EditionMismatchEduToVanilla;
                return true;
            case "EditorMismatchEditorToVanilla":
                result = DisconnectFailReason.EditorMismatchEditorToVanilla;
                return true;
            case "EditorMismatchVanillaToEditor":
                result = DisconnectFailReason.EditorMismatchVanillaToEditor;
                return true;
            case "DenyListed":
                result = DisconnectFailReason.DenyListed;
                return true;
            case "NonceMissing":
                result = DisconnectFailReason.NonceMissing;
                return true;
            case "NonceNotFound":
                result = DisconnectFailReason.NonceNotFound;
                return true;
            case "NonceExpired":
                result = DisconnectFailReason.NonceExpired;
                return true;
            case "NonceNotValid":
                result = DisconnectFailReason.NonceNotValid;
                return true;
            case "HostDisconnected":
                result = DisconnectFailReason.HostDisconnected;
                return true;
            case "EditorJoinIntentPolicyFailure":
                result = DisconnectFailReason.EditorJoinIntentPolicyFailure;
                return true;
            case "NetherNetIdentityNotAllowed":
                result = DisconnectFailReason.NetherNetIdentityNotAllowed;
                return true;
            case "InvalidName":
                result = DisconnectFailReason.InvalidName;
                return true;
            case "ExpiredToken":
                result = DisconnectFailReason.ExpiredToken;
                return true;
            case "HostAcceptsNoTypeOfAuth":
                result = DisconnectFailReason.HostAcceptsNoTypeOfAuth;
                return true;
            case "NotAuthenticatedFastFail":
                result = DisconnectFailReason.NotAuthenticatedFastFail;
                return true;
            case "EditorNotAllowed":
                result = DisconnectFailReason.EditorNotAllowed;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

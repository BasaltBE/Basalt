#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum AchievementIds {
    ChestFullOfCobblestone = 7,
    DiamondForYou = 10,
    IronBelly = 20,
    IronMan = 21,
    OnARail = 29,
    Overkill = 30,
    ReturnToSender = 37,
    SniperDuel = 38,
    StayinFrosty = 39,
    TakeInventory = 40,
    MapRoom = 50,
    FreightStation = 52,
    SmeltEverything = 53,
    TasteOfYourOwnMedicine = 54,
    WhenPigsFly = 56,
    Inception = 58,
    ArtificialSelection = 60,
    FreeDiver = 61,
    SpawnTheWither = 62,
    Beaconator = 63,
    GreatView = 64,
    SuperSonic = 65,
    TheEndAgain = 66,
    TreasureHunter = 67,
    ShootingStar = 68,
    FashionShow = 69,
    SelfPublishedAuthor = 71,
    AlternativeFuel = 72,
    SleepWithTheFishes = 73,
    Castaway = 74,
    ImAMarineBiologist = 75,
    SailThe7Seas = 76,
    MeGold = 77,
    Ahoy = 78,
    Atlantis = 79,
    OnePickleTwoPickleSeaPickleFour = 80,
    DoaBarrelRoll = 81,
    Moskstraumen = 82,
    Echolocation = 83,
    WhereHaveYouBeen = 84,
    TopOfTheWorld = 85,
    FruitOnTheLoom = 86,
    SoundTheAlarm = 87,
    BuyLowSellHigh = 88,
    Disenchanted = 89,
    TimeForStew = 90,
    BeeOurGuest = 91,
    TotalBeeLocation = 92,
    StickySituation = 93,
    CoverMeInDebris = 94,
    FloatYourGoat = 95,
    Friend = 96,
    WaxOnWaxOff = 97,
    StriderRiddenInLavaInOverworld = 98,
    GoatHornAcquired = 99,
    JukeboxUsedInMeadows = 100,
    TradedAtWorldHeight = 101,
    SurvivedFallFromWorldHeight = 102,
    SneakCloseToSculkSensor = 103,
    ItSpreads = 104,
    BirthdaySong = 105,
    WithOurPowersCombined = 106,
    PlantingThePast = 107,
    CarefulRestoration = 108,
    Revaulting = 109,
    CraftersCraftingCrafters = 110,
    WhoNeedsRockets = 111,
    OverOverkill = 112,
    HeartTransplanter = 113,
    StayHydrated = 114,
    MobKabob = 115,
    AdventuringTime = 116,
    UhOh = 117,
    GettingWood = 118,
    BenchMaking = 119,
    TimeToMine = 120,
    HotTopic = 121,
    AcquireHardware = 122,
    GettingAnUpgrade = 123,
    MonsterHunter = 124,
    Diamonds = 125,
    PlethoraOfCats = 126,
}

public static class AchievementIdsExtensions {
    public static string ToProtoString(this AchievementIds value) => value.ToProtocolString();

    public static string ToProtocolString(this AchievementIds value) {
        return value switch {
            AchievementIds.ChestFullOfCobblestone => "ChestFullOfCobblestone",
            AchievementIds.DiamondForYou => "DiamondForYou",
            AchievementIds.IronBelly => "IronBelly",
            AchievementIds.IronMan => "IronMan",
            AchievementIds.OnARail => "OnARail",
            AchievementIds.Overkill => "Overkill",
            AchievementIds.ReturnToSender => "ReturnToSender",
            AchievementIds.SniperDuel => "SniperDuel",
            AchievementIds.StayinFrosty => "StayinFrosty",
            AchievementIds.TakeInventory => "TakeInventory",
            AchievementIds.MapRoom => "MapRoom",
            AchievementIds.FreightStation => "FreightStation",
            AchievementIds.SmeltEverything => "SmeltEverything",
            AchievementIds.TasteOfYourOwnMedicine => "TasteOfYourOwnMedicine",
            AchievementIds.WhenPigsFly => "WhenPigsFly",
            AchievementIds.Inception => "Inception",
            AchievementIds.ArtificialSelection => "ArtificialSelection",
            AchievementIds.FreeDiver => "FreeDiver",
            AchievementIds.SpawnTheWither => "SpawnTheWither",
            AchievementIds.Beaconator => "Beaconator",
            AchievementIds.GreatView => "GreatView",
            AchievementIds.SuperSonic => "SuperSonic",
            AchievementIds.TheEndAgain => "TheEndAgain",
            AchievementIds.TreasureHunter => "TreasureHunter",
            AchievementIds.ShootingStar => "ShootingStar",
            AchievementIds.FashionShow => "FashionShow",
            AchievementIds.SelfPublishedAuthor => "SelfPublishedAuthor",
            AchievementIds.AlternativeFuel => "AlternativeFuel",
            AchievementIds.SleepWithTheFishes => "SleepWithTheFishes",
            AchievementIds.Castaway => "Castaway",
            AchievementIds.ImAMarineBiologist => "ImAMarineBiologist",
            AchievementIds.SailThe7Seas => "SailThe7Seas",
            AchievementIds.MeGold => "MeGold",
            AchievementIds.Ahoy => "Ahoy",
            AchievementIds.Atlantis => "Atlantis",
            AchievementIds.OnePickleTwoPickleSeaPickleFour => "OnePickleTwoPickleSeaPickleFour",
            AchievementIds.DoaBarrelRoll => "DoaBarrelRoll",
            AchievementIds.Moskstraumen => "Moskstraumen",
            AchievementIds.Echolocation => "Echolocation",
            AchievementIds.WhereHaveYouBeen => "WhereHaveYouBeen",
            AchievementIds.TopOfTheWorld => "TopOfTheWorld",
            AchievementIds.FruitOnTheLoom => "FruitOnTheLoom",
            AchievementIds.SoundTheAlarm => "SoundTheAlarm",
            AchievementIds.BuyLowSellHigh => "BuyLowSellHigh",
            AchievementIds.Disenchanted => "Disenchanted",
            AchievementIds.TimeForStew => "TimeForStew",
            AchievementIds.BeeOurGuest => "BeeOurGuest",
            AchievementIds.TotalBeeLocation => "TotalBeeLocation",
            AchievementIds.StickySituation => "StickySituation",
            AchievementIds.CoverMeInDebris => "CoverMeInDebris",
            AchievementIds.FloatYourGoat => "FloatYourGoat",
            AchievementIds.Friend => "Friend",
            AchievementIds.WaxOnWaxOff => "WaxOnWaxOff",
            AchievementIds.StriderRiddenInLavaInOverworld => "StriderRiddenInLavaInOverworld",
            AchievementIds.GoatHornAcquired => "GoatHornAcquired",
            AchievementIds.JukeboxUsedInMeadows => "JukeboxUsedInMeadows",
            AchievementIds.TradedAtWorldHeight => "TradedAtWorldHeight",
            AchievementIds.SurvivedFallFromWorldHeight => "SurvivedFallFromWorldHeight",
            AchievementIds.SneakCloseToSculkSensor => "SneakCloseToSculkSensor",
            AchievementIds.ItSpreads => "ItSpreads",
            AchievementIds.BirthdaySong => "BirthdaySong",
            AchievementIds.WithOurPowersCombined => "WithOurPowersCombined",
            AchievementIds.PlantingThePast => "PlantingThePast",
            AchievementIds.CarefulRestoration => "CarefulRestoration",
            AchievementIds.Revaulting => "Revaulting",
            AchievementIds.CraftersCraftingCrafters => "CraftersCraftingCrafters",
            AchievementIds.WhoNeedsRockets => "WhoNeedsRockets",
            AchievementIds.OverOverkill => "OverOverkill",
            AchievementIds.HeartTransplanter => "HeartTransplanter",
            AchievementIds.StayHydrated => "StayHydrated",
            AchievementIds.MobKabob => "MobKabob",
            AchievementIds.AdventuringTime => "AdventuringTime",
            AchievementIds.UhOh => "UhOh",
            AchievementIds.GettingWood => "GettingWood",
            AchievementIds.BenchMaking => "BenchMaking",
            AchievementIds.TimeToMine => "TimeToMine",
            AchievementIds.HotTopic => "HotTopic",
            AchievementIds.AcquireHardware => "AcquireHardware",
            AchievementIds.GettingAnUpgrade => "GettingAnUpgrade",
            AchievementIds.MonsterHunter => "MonsterHunter",
            AchievementIds.Diamonds => "Diamonds",
            AchievementIds.PlethoraOfCats => "PlethoraOfCats",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown AchievementIds value.")
        };
    }

    public static AchievementIds FromProtocolString(string value) {
        return value switch {
            "ChestFullOfCobblestone" => AchievementIds.ChestFullOfCobblestone,
            "DiamondForYou" => AchievementIds.DiamondForYou,
            "IronBelly" => AchievementIds.IronBelly,
            "IronMan" => AchievementIds.IronMan,
            "OnARail" => AchievementIds.OnARail,
            "Overkill" => AchievementIds.Overkill,
            "ReturnToSender" => AchievementIds.ReturnToSender,
            "SniperDuel" => AchievementIds.SniperDuel,
            "StayinFrosty" => AchievementIds.StayinFrosty,
            "TakeInventory" => AchievementIds.TakeInventory,
            "MapRoom" => AchievementIds.MapRoom,
            "FreightStation" => AchievementIds.FreightStation,
            "SmeltEverything" => AchievementIds.SmeltEverything,
            "TasteOfYourOwnMedicine" => AchievementIds.TasteOfYourOwnMedicine,
            "WhenPigsFly" => AchievementIds.WhenPigsFly,
            "Inception" => AchievementIds.Inception,
            "ArtificialSelection" => AchievementIds.ArtificialSelection,
            "FreeDiver" => AchievementIds.FreeDiver,
            "SpawnTheWither" => AchievementIds.SpawnTheWither,
            "Beaconator" => AchievementIds.Beaconator,
            "GreatView" => AchievementIds.GreatView,
            "SuperSonic" => AchievementIds.SuperSonic,
            "TheEndAgain" => AchievementIds.TheEndAgain,
            "TreasureHunter" => AchievementIds.TreasureHunter,
            "ShootingStar" => AchievementIds.ShootingStar,
            "FashionShow" => AchievementIds.FashionShow,
            "SelfPublishedAuthor" => AchievementIds.SelfPublishedAuthor,
            "AlternativeFuel" => AchievementIds.AlternativeFuel,
            "SleepWithTheFishes" => AchievementIds.SleepWithTheFishes,
            "Castaway" => AchievementIds.Castaway,
            "ImAMarineBiologist" => AchievementIds.ImAMarineBiologist,
            "SailThe7Seas" => AchievementIds.SailThe7Seas,
            "MeGold" => AchievementIds.MeGold,
            "Ahoy" => AchievementIds.Ahoy,
            "Atlantis" => AchievementIds.Atlantis,
            "OnePickleTwoPickleSeaPickleFour" => AchievementIds.OnePickleTwoPickleSeaPickleFour,
            "DoaBarrelRoll" => AchievementIds.DoaBarrelRoll,
            "Moskstraumen" => AchievementIds.Moskstraumen,
            "Echolocation" => AchievementIds.Echolocation,
            "WhereHaveYouBeen" => AchievementIds.WhereHaveYouBeen,
            "TopOfTheWorld" => AchievementIds.TopOfTheWorld,
            "FruitOnTheLoom" => AchievementIds.FruitOnTheLoom,
            "SoundTheAlarm" => AchievementIds.SoundTheAlarm,
            "BuyLowSellHigh" => AchievementIds.BuyLowSellHigh,
            "Disenchanted" => AchievementIds.Disenchanted,
            "TimeForStew" => AchievementIds.TimeForStew,
            "BeeOurGuest" => AchievementIds.BeeOurGuest,
            "TotalBeeLocation" => AchievementIds.TotalBeeLocation,
            "StickySituation" => AchievementIds.StickySituation,
            "CoverMeInDebris" => AchievementIds.CoverMeInDebris,
            "FloatYourGoat" => AchievementIds.FloatYourGoat,
            "Friend" => AchievementIds.Friend,
            "WaxOnWaxOff" => AchievementIds.WaxOnWaxOff,
            "StriderRiddenInLavaInOverworld" => AchievementIds.StriderRiddenInLavaInOverworld,
            "GoatHornAcquired" => AchievementIds.GoatHornAcquired,
            "JukeboxUsedInMeadows" => AchievementIds.JukeboxUsedInMeadows,
            "TradedAtWorldHeight" => AchievementIds.TradedAtWorldHeight,
            "SurvivedFallFromWorldHeight" => AchievementIds.SurvivedFallFromWorldHeight,
            "SneakCloseToSculkSensor" => AchievementIds.SneakCloseToSculkSensor,
            "ItSpreads" => AchievementIds.ItSpreads,
            "BirthdaySong" => AchievementIds.BirthdaySong,
            "WithOurPowersCombined" => AchievementIds.WithOurPowersCombined,
            "PlantingThePast" => AchievementIds.PlantingThePast,
            "CarefulRestoration" => AchievementIds.CarefulRestoration,
            "Revaulting" => AchievementIds.Revaulting,
            "CraftersCraftingCrafters" => AchievementIds.CraftersCraftingCrafters,
            "WhoNeedsRockets" => AchievementIds.WhoNeedsRockets,
            "OverOverkill" => AchievementIds.OverOverkill,
            "HeartTransplanter" => AchievementIds.HeartTransplanter,
            "StayHydrated" => AchievementIds.StayHydrated,
            "MobKabob" => AchievementIds.MobKabob,
            "AdventuringTime" => AchievementIds.AdventuringTime,
            "UhOh" => AchievementIds.UhOh,
            "GettingWood" => AchievementIds.GettingWood,
            "BenchMaking" => AchievementIds.BenchMaking,
            "TimeToMine" => AchievementIds.TimeToMine,
            "HotTopic" => AchievementIds.HotTopic,
            "AcquireHardware" => AchievementIds.AcquireHardware,
            "GettingAnUpgrade" => AchievementIds.GettingAnUpgrade,
            "MonsterHunter" => AchievementIds.MonsterHunter,
            "Diamonds" => AchievementIds.Diamonds,
            "PlethoraOfCats" => AchievementIds.PlethoraOfCats,
            _ => throw new ArgumentException($"Unknown AchievementIds protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out AchievementIds result) {
        switch (value) {
            case "ChestFullOfCobblestone":
                result = AchievementIds.ChestFullOfCobblestone;
                return true;
            case "DiamondForYou":
                result = AchievementIds.DiamondForYou;
                return true;
            case "IronBelly":
                result = AchievementIds.IronBelly;
                return true;
            case "IronMan":
                result = AchievementIds.IronMan;
                return true;
            case "OnARail":
                result = AchievementIds.OnARail;
                return true;
            case "Overkill":
                result = AchievementIds.Overkill;
                return true;
            case "ReturnToSender":
                result = AchievementIds.ReturnToSender;
                return true;
            case "SniperDuel":
                result = AchievementIds.SniperDuel;
                return true;
            case "StayinFrosty":
                result = AchievementIds.StayinFrosty;
                return true;
            case "TakeInventory":
                result = AchievementIds.TakeInventory;
                return true;
            case "MapRoom":
                result = AchievementIds.MapRoom;
                return true;
            case "FreightStation":
                result = AchievementIds.FreightStation;
                return true;
            case "SmeltEverything":
                result = AchievementIds.SmeltEverything;
                return true;
            case "TasteOfYourOwnMedicine":
                result = AchievementIds.TasteOfYourOwnMedicine;
                return true;
            case "WhenPigsFly":
                result = AchievementIds.WhenPigsFly;
                return true;
            case "Inception":
                result = AchievementIds.Inception;
                return true;
            case "ArtificialSelection":
                result = AchievementIds.ArtificialSelection;
                return true;
            case "FreeDiver":
                result = AchievementIds.FreeDiver;
                return true;
            case "SpawnTheWither":
                result = AchievementIds.SpawnTheWither;
                return true;
            case "Beaconator":
                result = AchievementIds.Beaconator;
                return true;
            case "GreatView":
                result = AchievementIds.GreatView;
                return true;
            case "SuperSonic":
                result = AchievementIds.SuperSonic;
                return true;
            case "TheEndAgain":
                result = AchievementIds.TheEndAgain;
                return true;
            case "TreasureHunter":
                result = AchievementIds.TreasureHunter;
                return true;
            case "ShootingStar":
                result = AchievementIds.ShootingStar;
                return true;
            case "FashionShow":
                result = AchievementIds.FashionShow;
                return true;
            case "SelfPublishedAuthor":
                result = AchievementIds.SelfPublishedAuthor;
                return true;
            case "AlternativeFuel":
                result = AchievementIds.AlternativeFuel;
                return true;
            case "SleepWithTheFishes":
                result = AchievementIds.SleepWithTheFishes;
                return true;
            case "Castaway":
                result = AchievementIds.Castaway;
                return true;
            case "ImAMarineBiologist":
                result = AchievementIds.ImAMarineBiologist;
                return true;
            case "SailThe7Seas":
                result = AchievementIds.SailThe7Seas;
                return true;
            case "MeGold":
                result = AchievementIds.MeGold;
                return true;
            case "Ahoy":
                result = AchievementIds.Ahoy;
                return true;
            case "Atlantis":
                result = AchievementIds.Atlantis;
                return true;
            case "OnePickleTwoPickleSeaPickleFour":
                result = AchievementIds.OnePickleTwoPickleSeaPickleFour;
                return true;
            case "DoaBarrelRoll":
                result = AchievementIds.DoaBarrelRoll;
                return true;
            case "Moskstraumen":
                result = AchievementIds.Moskstraumen;
                return true;
            case "Echolocation":
                result = AchievementIds.Echolocation;
                return true;
            case "WhereHaveYouBeen":
                result = AchievementIds.WhereHaveYouBeen;
                return true;
            case "TopOfTheWorld":
                result = AchievementIds.TopOfTheWorld;
                return true;
            case "FruitOnTheLoom":
                result = AchievementIds.FruitOnTheLoom;
                return true;
            case "SoundTheAlarm":
                result = AchievementIds.SoundTheAlarm;
                return true;
            case "BuyLowSellHigh":
                result = AchievementIds.BuyLowSellHigh;
                return true;
            case "Disenchanted":
                result = AchievementIds.Disenchanted;
                return true;
            case "TimeForStew":
                result = AchievementIds.TimeForStew;
                return true;
            case "BeeOurGuest":
                result = AchievementIds.BeeOurGuest;
                return true;
            case "TotalBeeLocation":
                result = AchievementIds.TotalBeeLocation;
                return true;
            case "StickySituation":
                result = AchievementIds.StickySituation;
                return true;
            case "CoverMeInDebris":
                result = AchievementIds.CoverMeInDebris;
                return true;
            case "FloatYourGoat":
                result = AchievementIds.FloatYourGoat;
                return true;
            case "Friend":
                result = AchievementIds.Friend;
                return true;
            case "WaxOnWaxOff":
                result = AchievementIds.WaxOnWaxOff;
                return true;
            case "StriderRiddenInLavaInOverworld":
                result = AchievementIds.StriderRiddenInLavaInOverworld;
                return true;
            case "GoatHornAcquired":
                result = AchievementIds.GoatHornAcquired;
                return true;
            case "JukeboxUsedInMeadows":
                result = AchievementIds.JukeboxUsedInMeadows;
                return true;
            case "TradedAtWorldHeight":
                result = AchievementIds.TradedAtWorldHeight;
                return true;
            case "SurvivedFallFromWorldHeight":
                result = AchievementIds.SurvivedFallFromWorldHeight;
                return true;
            case "SneakCloseToSculkSensor":
                result = AchievementIds.SneakCloseToSculkSensor;
                return true;
            case "ItSpreads":
                result = AchievementIds.ItSpreads;
                return true;
            case "BirthdaySong":
                result = AchievementIds.BirthdaySong;
                return true;
            case "WithOurPowersCombined":
                result = AchievementIds.WithOurPowersCombined;
                return true;
            case "PlantingThePast":
                result = AchievementIds.PlantingThePast;
                return true;
            case "CarefulRestoration":
                result = AchievementIds.CarefulRestoration;
                return true;
            case "Revaulting":
                result = AchievementIds.Revaulting;
                return true;
            case "CraftersCraftingCrafters":
                result = AchievementIds.CraftersCraftingCrafters;
                return true;
            case "WhoNeedsRockets":
                result = AchievementIds.WhoNeedsRockets;
                return true;
            case "OverOverkill":
                result = AchievementIds.OverOverkill;
                return true;
            case "HeartTransplanter":
                result = AchievementIds.HeartTransplanter;
                return true;
            case "StayHydrated":
                result = AchievementIds.StayHydrated;
                return true;
            case "MobKabob":
                result = AchievementIds.MobKabob;
                return true;
            case "AdventuringTime":
                result = AchievementIds.AdventuringTime;
                return true;
            case "UhOh":
                result = AchievementIds.UhOh;
                return true;
            case "GettingWood":
                result = AchievementIds.GettingWood;
                return true;
            case "BenchMaking":
                result = AchievementIds.BenchMaking;
                return true;
            case "TimeToMine":
                result = AchievementIds.TimeToMine;
                return true;
            case "HotTopic":
                result = AchievementIds.HotTopic;
                return true;
            case "AcquireHardware":
                result = AchievementIds.AcquireHardware;
                return true;
            case "GettingAnUpgrade":
                result = AchievementIds.GettingAnUpgrade;
                return true;
            case "MonsterHunter":
                result = AchievementIds.MonsterHunter;
                return true;
            case "Diamonds":
                result = AchievementIds.Diamonds;
                return true;
            case "PlethoraOfCats":
                result = AchievementIds.PlethoraOfCats;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

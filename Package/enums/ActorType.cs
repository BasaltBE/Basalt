#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ActorType {
    Undefined = 1,
    Mob = 256,
    PathfinderMob = 768,
    Monster = 2816,
    Animal = 4864,
    TamableAnimal = 21248,
    Ambient = 33024,
    UndeadMonster = 68352,
    ZombieMonster = 199424,
    Arthropod = 264960,
    Minecart = 524288,
    SkeletonMonster = 1116928,
    EquineAnimal = 2118400,
    Projectile = 4194304,
    AbstractArrow = 8388608,
    WaterAnimal = 8960,
    VillagerBase = 16777984,
    Chicken = 4874,
    Cow = 4875,
    Pig = 4876,
    Sheep = 4877,
    Wolf = 21262,
    Villager = 16777999,
    MushroomCow = 4880,
    Squid = 8977,
    Rabbit = 4882,
    Bat = 33043,
    IronGolem = 788,
    SnowGolem = 789,
    Ocelot = 21270,
    Horse = 2118423,
    PolarBear = 4892,
    Llama = 4893,
    Parrot = 21278,
    Dolphin = 8991,
    Donkey = 2118424,
    Mule = 2118425,
    SkeletonHorse = 2183962,
    ZombieHorse = 2183963,
    Zombie = 199456,
    Creeper = 2849,
    Skeleton = 1116962,
    Spider = 264995,
    PigZombie = 68388,
    Slime = 2853,
    EnderMan = 2854,
    Silverfish = 264999,
    CaveSpider = 265000,
    Ghast = 2857,
    LavaSlime = 2858,
    Blaze = 2859,
    ZombieVillager = 199468,
    Witch = 2861,
    Stray = 1116974,
    Husk = 199471,
    WitherSkeleton = 1116976,
    Guardian = 2865,
    ElderGuardian = 2866,
    Npc = 307,
    WitherBoss = 68404,
    Dragon = 2869,
    Shulker = 2870,
    Endermite = 265015,
    Agent = 312,
    Vindicator = 2873,
    Phantom = 68410,
    IllagerBeast = 2875,
    ArmorStand = 317,
    TripodCamera = 318,
    Player = 319,
    ItemEntity = 64,
    PrimedTnt = 65,
    FallingBlock = 66,
    MovingBlock = 67,
    ExperiencePotion = 4194372,
    Experience = 69,
    EyeOfEnder = 70,
    EnderCrystal = 71,
    FireworksRocket = 72,
    Trident = 12582985,
    Turtle = 4938,
    Cat = 21323,
    ShulkerBullet = 4194380,
    FishingHook = 77,
    Chalkboard = 78,
    DragonFireball = 4194383,
    Arrow = 12582992,
    Snowball = 4194385,
    ThrownEgg = 4194386,
    Painting = 83,
    LargeFireball = 4194389,
    ThrownPotion = 4194390,
    Enderpearl = 4194391,
    LeashKnot = 88,
    WitherSkull = 4194393,
    BoatRideable = 90,
    WitherSkullDangerous = 4194395,
    LightningBolt = 93,
    SmallFireball = 4194398,
    AreaEffectCloud = 95,
    LingeringPotion = 4194405,
    LlamaSpit = 4194406,
    EvocationFang = 4194407,
    EvocationIllager = 2920,
    Vex = 2921,
    MinecartRideable = 524372,
    MinecartHopper = 524384,
    MinecartTNT = 524385,
    MinecartChest = 524386,
    MinecartFurnace = 524387,
    MinecartCommandBlock = 524388,
    IceBomb = 4194410,
    Balloon = 107,
    Pufferfish = 9068,
    Salmon = 9069,
    Drowned = 199534,
    Tropicalfish = 9071,
    Fish = 9072,
    Panda = 4977,
    Pillager = 2930,
    VillagerV2 = 16778099,
    ZombieVillagerV2 = 199540,
    Shield = 117,
    WanderingTrader = 886,
    Lectern = 119,
    ElderGuardianGhost = 2936,
    Fox = 4985,
    Bee = 378,
    Piglin = 379,
    Hoglin = 4988,
    Strider = 4989,
    Zoglin = 68478,
    PiglinBrute = 383,
    Goat = 4992,
    GlowSquid = 9089,
    Axolotl = 4994,
    Warden = 2947,
    Frog = 4996,
    Tadpole = 9093,
    Allay = 390,
    ChestBoatRideable = 218,
    TraderLlama = 5021,
    Camel = 5002,
    Sniffer = 5003,
    Breeze = 2956,
    BreezeWindChargeProjectile = 4194445,
    Armadillo = 5006,
    WindChargeProjectile = 4194447,
    Bogged = 1117072,
    OminousItemSpawner = 145,
    Creaking = 2962,
    HappyGhast = 5011,
    CopperGolem = 916,
    Nautilus = 9109,
    ZombieNautilus = 74646,
    Parched = 1117079,
    CamelHusk = 70552,
    SulfurCube = 921,
    Cushion = 154,
}

public static class ActorTypeExtensions {
    public static string ToProtoString(this ActorType value) => value.ToProtocolString();

    public static string ToProtocolString(this ActorType value) {
        return value switch {
            ActorType.Undefined => "Undefined",
            ActorType.Mob => "Mob",
            ActorType.PathfinderMob => "PathfinderMob",
            ActorType.Monster => "Monster",
            ActorType.Animal => "Animal",
            ActorType.TamableAnimal => "TamableAnimal",
            ActorType.Ambient => "Ambient",
            ActorType.UndeadMonster => "UndeadMonster",
            ActorType.ZombieMonster => "ZombieMonster",
            ActorType.Arthropod => "Arthropod",
            ActorType.Minecart => "Minecart",
            ActorType.SkeletonMonster => "SkeletonMonster",
            ActorType.EquineAnimal => "EquineAnimal",
            ActorType.Projectile => "Projectile",
            ActorType.AbstractArrow => "AbstractArrow",
            ActorType.WaterAnimal => "WaterAnimal",
            ActorType.VillagerBase => "VillagerBase",
            ActorType.Chicken => "Chicken",
            ActorType.Cow => "Cow",
            ActorType.Pig => "Pig",
            ActorType.Sheep => "Sheep",
            ActorType.Wolf => "Wolf",
            ActorType.Villager => "Villager",
            ActorType.MushroomCow => "MushroomCow",
            ActorType.Squid => "Squid",
            ActorType.Rabbit => "Rabbit",
            ActorType.Bat => "Bat",
            ActorType.IronGolem => "IronGolem",
            ActorType.SnowGolem => "SnowGolem",
            ActorType.Ocelot => "Ocelot",
            ActorType.Horse => "Horse",
            ActorType.PolarBear => "PolarBear",
            ActorType.Llama => "Llama",
            ActorType.Parrot => "Parrot",
            ActorType.Dolphin => "Dolphin",
            ActorType.Donkey => "Donkey",
            ActorType.Mule => "Mule",
            ActorType.SkeletonHorse => "SkeletonHorse",
            ActorType.ZombieHorse => "ZombieHorse",
            ActorType.Zombie => "Zombie",
            ActorType.Creeper => "Creeper",
            ActorType.Skeleton => "Skeleton",
            ActorType.Spider => "Spider",
            ActorType.PigZombie => "PigZombie",
            ActorType.Slime => "Slime",
            ActorType.EnderMan => "EnderMan",
            ActorType.Silverfish => "Silverfish",
            ActorType.CaveSpider => "CaveSpider",
            ActorType.Ghast => "Ghast",
            ActorType.LavaSlime => "LavaSlime",
            ActorType.Blaze => "Blaze",
            ActorType.ZombieVillager => "ZombieVillager",
            ActorType.Witch => "Witch",
            ActorType.Stray => "Stray",
            ActorType.Husk => "Husk",
            ActorType.WitherSkeleton => "WitherSkeleton",
            ActorType.Guardian => "Guardian",
            ActorType.ElderGuardian => "ElderGuardian",
            ActorType.Npc => "Npc",
            ActorType.WitherBoss => "WitherBoss",
            ActorType.Dragon => "Dragon",
            ActorType.Shulker => "Shulker",
            ActorType.Endermite => "Endermite",
            ActorType.Agent => "Agent",
            ActorType.Vindicator => "Vindicator",
            ActorType.Phantom => "Phantom",
            ActorType.IllagerBeast => "IllagerBeast",
            ActorType.ArmorStand => "ArmorStand",
            ActorType.TripodCamera => "TripodCamera",
            ActorType.Player => "Player",
            ActorType.ItemEntity => "ItemEntity",
            ActorType.PrimedTnt => "PrimedTnt",
            ActorType.FallingBlock => "FallingBlock",
            ActorType.MovingBlock => "MovingBlock",
            ActorType.ExperiencePotion => "ExperiencePotion",
            ActorType.Experience => "Experience",
            ActorType.EyeOfEnder => "EyeOfEnder",
            ActorType.EnderCrystal => "EnderCrystal",
            ActorType.FireworksRocket => "FireworksRocket",
            ActorType.Trident => "Trident",
            ActorType.Turtle => "Turtle",
            ActorType.Cat => "Cat",
            ActorType.ShulkerBullet => "ShulkerBullet",
            ActorType.FishingHook => "FishingHook",
            ActorType.Chalkboard => "Chalkboard",
            ActorType.DragonFireball => "DragonFireball",
            ActorType.Arrow => "Arrow",
            ActorType.Snowball => "Snowball",
            ActorType.ThrownEgg => "ThrownEgg",
            ActorType.Painting => "Painting",
            ActorType.LargeFireball => "LargeFireball",
            ActorType.ThrownPotion => "ThrownPotion",
            ActorType.Enderpearl => "Enderpearl",
            ActorType.LeashKnot => "LeashKnot",
            ActorType.WitherSkull => "WitherSkull",
            ActorType.BoatRideable => "BoatRideable",
            ActorType.WitherSkullDangerous => "WitherSkullDangerous",
            ActorType.LightningBolt => "LightningBolt",
            ActorType.SmallFireball => "SmallFireball",
            ActorType.AreaEffectCloud => "AreaEffectCloud",
            ActorType.LingeringPotion => "LingeringPotion",
            ActorType.LlamaSpit => "LlamaSpit",
            ActorType.EvocationFang => "EvocationFang",
            ActorType.EvocationIllager => "EvocationIllager",
            ActorType.Vex => "Vex",
            ActorType.MinecartRideable => "MinecartRideable",
            ActorType.MinecartHopper => "MinecartHopper",
            ActorType.MinecartTNT => "MinecartTNT",
            ActorType.MinecartChest => "MinecartChest",
            ActorType.MinecartFurnace => "MinecartFurnace",
            ActorType.MinecartCommandBlock => "MinecartCommandBlock",
            ActorType.IceBomb => "IceBomb",
            ActorType.Balloon => "Balloon",
            ActorType.Pufferfish => "Pufferfish",
            ActorType.Salmon => "Salmon",
            ActorType.Drowned => "Drowned",
            ActorType.Tropicalfish => "Tropicalfish",
            ActorType.Fish => "Fish",
            ActorType.Panda => "Panda",
            ActorType.Pillager => "Pillager",
            ActorType.VillagerV2 => "VillagerV2",
            ActorType.ZombieVillagerV2 => "ZombieVillagerV2",
            ActorType.Shield => "Shield",
            ActorType.WanderingTrader => "WanderingTrader",
            ActorType.Lectern => "Lectern",
            ActorType.ElderGuardianGhost => "ElderGuardianGhost",
            ActorType.Fox => "Fox",
            ActorType.Bee => "Bee",
            ActorType.Piglin => "Piglin",
            ActorType.Hoglin => "Hoglin",
            ActorType.Strider => "Strider",
            ActorType.Zoglin => "Zoglin",
            ActorType.PiglinBrute => "PiglinBrute",
            ActorType.Goat => "Goat",
            ActorType.GlowSquid => "GlowSquid",
            ActorType.Axolotl => "Axolotl",
            ActorType.Warden => "Warden",
            ActorType.Frog => "Frog",
            ActorType.Tadpole => "Tadpole",
            ActorType.Allay => "Allay",
            ActorType.ChestBoatRideable => "ChestBoatRideable",
            ActorType.TraderLlama => "TraderLlama",
            ActorType.Camel => "Camel",
            ActorType.Sniffer => "Sniffer",
            ActorType.Breeze => "Breeze",
            ActorType.BreezeWindChargeProjectile => "BreezeWindChargeProjectile",
            ActorType.Armadillo => "Armadillo",
            ActorType.WindChargeProjectile => "WindChargeProjectile",
            ActorType.Bogged => "Bogged",
            ActorType.OminousItemSpawner => "OminousItemSpawner",
            ActorType.Creaking => "Creaking",
            ActorType.HappyGhast => "HappyGhast",
            ActorType.CopperGolem => "CopperGolem",
            ActorType.Nautilus => "Nautilus",
            ActorType.ZombieNautilus => "ZombieNautilus",
            ActorType.Parched => "Parched",
            ActorType.CamelHusk => "CamelHusk",
            ActorType.SulfurCube => "SulfurCube",
            ActorType.Cushion => "Cushion",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ActorType value.")
        };
    }

    public static ActorType FromProtocolString(string value) {
        return value switch {
            "Undefined" => ActorType.Undefined,
            "Mob" => ActorType.Mob,
            "PathfinderMob" => ActorType.PathfinderMob,
            "Monster" => ActorType.Monster,
            "Animal" => ActorType.Animal,
            "TamableAnimal" => ActorType.TamableAnimal,
            "Ambient" => ActorType.Ambient,
            "UndeadMonster" => ActorType.UndeadMonster,
            "ZombieMonster" => ActorType.ZombieMonster,
            "Arthropod" => ActorType.Arthropod,
            "Minecart" => ActorType.Minecart,
            "SkeletonMonster" => ActorType.SkeletonMonster,
            "EquineAnimal" => ActorType.EquineAnimal,
            "Projectile" => ActorType.Projectile,
            "AbstractArrow" => ActorType.AbstractArrow,
            "WaterAnimal" => ActorType.WaterAnimal,
            "VillagerBase" => ActorType.VillagerBase,
            "Chicken" => ActorType.Chicken,
            "Cow" => ActorType.Cow,
            "Pig" => ActorType.Pig,
            "Sheep" => ActorType.Sheep,
            "Wolf" => ActorType.Wolf,
            "Villager" => ActorType.Villager,
            "MushroomCow" => ActorType.MushroomCow,
            "Squid" => ActorType.Squid,
            "Rabbit" => ActorType.Rabbit,
            "Bat" => ActorType.Bat,
            "IronGolem" => ActorType.IronGolem,
            "SnowGolem" => ActorType.SnowGolem,
            "Ocelot" => ActorType.Ocelot,
            "Horse" => ActorType.Horse,
            "PolarBear" => ActorType.PolarBear,
            "Llama" => ActorType.Llama,
            "Parrot" => ActorType.Parrot,
            "Dolphin" => ActorType.Dolphin,
            "Donkey" => ActorType.Donkey,
            "Mule" => ActorType.Mule,
            "SkeletonHorse" => ActorType.SkeletonHorse,
            "ZombieHorse" => ActorType.ZombieHorse,
            "Zombie" => ActorType.Zombie,
            "Creeper" => ActorType.Creeper,
            "Skeleton" => ActorType.Skeleton,
            "Spider" => ActorType.Spider,
            "PigZombie" => ActorType.PigZombie,
            "Slime" => ActorType.Slime,
            "EnderMan" => ActorType.EnderMan,
            "Silverfish" => ActorType.Silverfish,
            "CaveSpider" => ActorType.CaveSpider,
            "Ghast" => ActorType.Ghast,
            "LavaSlime" => ActorType.LavaSlime,
            "Blaze" => ActorType.Blaze,
            "ZombieVillager" => ActorType.ZombieVillager,
            "Witch" => ActorType.Witch,
            "Stray" => ActorType.Stray,
            "Husk" => ActorType.Husk,
            "WitherSkeleton" => ActorType.WitherSkeleton,
            "Guardian" => ActorType.Guardian,
            "ElderGuardian" => ActorType.ElderGuardian,
            "Npc" => ActorType.Npc,
            "WitherBoss" => ActorType.WitherBoss,
            "Dragon" => ActorType.Dragon,
            "Shulker" => ActorType.Shulker,
            "Endermite" => ActorType.Endermite,
            "Agent" => ActorType.Agent,
            "Vindicator" => ActorType.Vindicator,
            "Phantom" => ActorType.Phantom,
            "IllagerBeast" => ActorType.IllagerBeast,
            "ArmorStand" => ActorType.ArmorStand,
            "TripodCamera" => ActorType.TripodCamera,
            "Player" => ActorType.Player,
            "ItemEntity" => ActorType.ItemEntity,
            "PrimedTnt" => ActorType.PrimedTnt,
            "FallingBlock" => ActorType.FallingBlock,
            "MovingBlock" => ActorType.MovingBlock,
            "ExperiencePotion" => ActorType.ExperiencePotion,
            "Experience" => ActorType.Experience,
            "EyeOfEnder" => ActorType.EyeOfEnder,
            "EnderCrystal" => ActorType.EnderCrystal,
            "FireworksRocket" => ActorType.FireworksRocket,
            "Trident" => ActorType.Trident,
            "Turtle" => ActorType.Turtle,
            "Cat" => ActorType.Cat,
            "ShulkerBullet" => ActorType.ShulkerBullet,
            "FishingHook" => ActorType.FishingHook,
            "Chalkboard" => ActorType.Chalkboard,
            "DragonFireball" => ActorType.DragonFireball,
            "Arrow" => ActorType.Arrow,
            "Snowball" => ActorType.Snowball,
            "ThrownEgg" => ActorType.ThrownEgg,
            "Painting" => ActorType.Painting,
            "LargeFireball" => ActorType.LargeFireball,
            "ThrownPotion" => ActorType.ThrownPotion,
            "Enderpearl" => ActorType.Enderpearl,
            "LeashKnot" => ActorType.LeashKnot,
            "WitherSkull" => ActorType.WitherSkull,
            "BoatRideable" => ActorType.BoatRideable,
            "WitherSkullDangerous" => ActorType.WitherSkullDangerous,
            "LightningBolt" => ActorType.LightningBolt,
            "SmallFireball" => ActorType.SmallFireball,
            "AreaEffectCloud" => ActorType.AreaEffectCloud,
            "LingeringPotion" => ActorType.LingeringPotion,
            "LlamaSpit" => ActorType.LlamaSpit,
            "EvocationFang" => ActorType.EvocationFang,
            "EvocationIllager" => ActorType.EvocationIllager,
            "Vex" => ActorType.Vex,
            "MinecartRideable" => ActorType.MinecartRideable,
            "MinecartHopper" => ActorType.MinecartHopper,
            "MinecartTNT" => ActorType.MinecartTNT,
            "MinecartChest" => ActorType.MinecartChest,
            "MinecartFurnace" => ActorType.MinecartFurnace,
            "MinecartCommandBlock" => ActorType.MinecartCommandBlock,
            "IceBomb" => ActorType.IceBomb,
            "Balloon" => ActorType.Balloon,
            "Pufferfish" => ActorType.Pufferfish,
            "Salmon" => ActorType.Salmon,
            "Drowned" => ActorType.Drowned,
            "Tropicalfish" => ActorType.Tropicalfish,
            "Fish" => ActorType.Fish,
            "Panda" => ActorType.Panda,
            "Pillager" => ActorType.Pillager,
            "VillagerV2" => ActorType.VillagerV2,
            "ZombieVillagerV2" => ActorType.ZombieVillagerV2,
            "Shield" => ActorType.Shield,
            "WanderingTrader" => ActorType.WanderingTrader,
            "Lectern" => ActorType.Lectern,
            "ElderGuardianGhost" => ActorType.ElderGuardianGhost,
            "Fox" => ActorType.Fox,
            "Bee" => ActorType.Bee,
            "Piglin" => ActorType.Piglin,
            "Hoglin" => ActorType.Hoglin,
            "Strider" => ActorType.Strider,
            "Zoglin" => ActorType.Zoglin,
            "PiglinBrute" => ActorType.PiglinBrute,
            "Goat" => ActorType.Goat,
            "GlowSquid" => ActorType.GlowSquid,
            "Axolotl" => ActorType.Axolotl,
            "Warden" => ActorType.Warden,
            "Frog" => ActorType.Frog,
            "Tadpole" => ActorType.Tadpole,
            "Allay" => ActorType.Allay,
            "ChestBoatRideable" => ActorType.ChestBoatRideable,
            "TraderLlama" => ActorType.TraderLlama,
            "Camel" => ActorType.Camel,
            "Sniffer" => ActorType.Sniffer,
            "Breeze" => ActorType.Breeze,
            "BreezeWindChargeProjectile" => ActorType.BreezeWindChargeProjectile,
            "Armadillo" => ActorType.Armadillo,
            "WindChargeProjectile" => ActorType.WindChargeProjectile,
            "Bogged" => ActorType.Bogged,
            "OminousItemSpawner" => ActorType.OminousItemSpawner,
            "Creaking" => ActorType.Creaking,
            "HappyGhast" => ActorType.HappyGhast,
            "CopperGolem" => ActorType.CopperGolem,
            "Nautilus" => ActorType.Nautilus,
            "ZombieNautilus" => ActorType.ZombieNautilus,
            "Parched" => ActorType.Parched,
            "CamelHusk" => ActorType.CamelHusk,
            "SulfurCube" => ActorType.SulfurCube,
            "Cushion" => ActorType.Cushion,
            _ => throw new ArgumentException($"Unknown ActorType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ActorType result) {
        switch (value) {
            case "Undefined":
                result = ActorType.Undefined;
                return true;
            case "Mob":
                result = ActorType.Mob;
                return true;
            case "PathfinderMob":
                result = ActorType.PathfinderMob;
                return true;
            case "Monster":
                result = ActorType.Monster;
                return true;
            case "Animal":
                result = ActorType.Animal;
                return true;
            case "TamableAnimal":
                result = ActorType.TamableAnimal;
                return true;
            case "Ambient":
                result = ActorType.Ambient;
                return true;
            case "UndeadMonster":
                result = ActorType.UndeadMonster;
                return true;
            case "ZombieMonster":
                result = ActorType.ZombieMonster;
                return true;
            case "Arthropod":
                result = ActorType.Arthropod;
                return true;
            case "Minecart":
                result = ActorType.Minecart;
                return true;
            case "SkeletonMonster":
                result = ActorType.SkeletonMonster;
                return true;
            case "EquineAnimal":
                result = ActorType.EquineAnimal;
                return true;
            case "Projectile":
                result = ActorType.Projectile;
                return true;
            case "AbstractArrow":
                result = ActorType.AbstractArrow;
                return true;
            case "WaterAnimal":
                result = ActorType.WaterAnimal;
                return true;
            case "VillagerBase":
                result = ActorType.VillagerBase;
                return true;
            case "Chicken":
                result = ActorType.Chicken;
                return true;
            case "Cow":
                result = ActorType.Cow;
                return true;
            case "Pig":
                result = ActorType.Pig;
                return true;
            case "Sheep":
                result = ActorType.Sheep;
                return true;
            case "Wolf":
                result = ActorType.Wolf;
                return true;
            case "Villager":
                result = ActorType.Villager;
                return true;
            case "MushroomCow":
                result = ActorType.MushroomCow;
                return true;
            case "Squid":
                result = ActorType.Squid;
                return true;
            case "Rabbit":
                result = ActorType.Rabbit;
                return true;
            case "Bat":
                result = ActorType.Bat;
                return true;
            case "IronGolem":
                result = ActorType.IronGolem;
                return true;
            case "SnowGolem":
                result = ActorType.SnowGolem;
                return true;
            case "Ocelot":
                result = ActorType.Ocelot;
                return true;
            case "Horse":
                result = ActorType.Horse;
                return true;
            case "PolarBear":
                result = ActorType.PolarBear;
                return true;
            case "Llama":
                result = ActorType.Llama;
                return true;
            case "Parrot":
                result = ActorType.Parrot;
                return true;
            case "Dolphin":
                result = ActorType.Dolphin;
                return true;
            case "Donkey":
                result = ActorType.Donkey;
                return true;
            case "Mule":
                result = ActorType.Mule;
                return true;
            case "SkeletonHorse":
                result = ActorType.SkeletonHorse;
                return true;
            case "ZombieHorse":
                result = ActorType.ZombieHorse;
                return true;
            case "Zombie":
                result = ActorType.Zombie;
                return true;
            case "Creeper":
                result = ActorType.Creeper;
                return true;
            case "Skeleton":
                result = ActorType.Skeleton;
                return true;
            case "Spider":
                result = ActorType.Spider;
                return true;
            case "PigZombie":
                result = ActorType.PigZombie;
                return true;
            case "Slime":
                result = ActorType.Slime;
                return true;
            case "EnderMan":
                result = ActorType.EnderMan;
                return true;
            case "Silverfish":
                result = ActorType.Silverfish;
                return true;
            case "CaveSpider":
                result = ActorType.CaveSpider;
                return true;
            case "Ghast":
                result = ActorType.Ghast;
                return true;
            case "LavaSlime":
                result = ActorType.LavaSlime;
                return true;
            case "Blaze":
                result = ActorType.Blaze;
                return true;
            case "ZombieVillager":
                result = ActorType.ZombieVillager;
                return true;
            case "Witch":
                result = ActorType.Witch;
                return true;
            case "Stray":
                result = ActorType.Stray;
                return true;
            case "Husk":
                result = ActorType.Husk;
                return true;
            case "WitherSkeleton":
                result = ActorType.WitherSkeleton;
                return true;
            case "Guardian":
                result = ActorType.Guardian;
                return true;
            case "ElderGuardian":
                result = ActorType.ElderGuardian;
                return true;
            case "Npc":
                result = ActorType.Npc;
                return true;
            case "WitherBoss":
                result = ActorType.WitherBoss;
                return true;
            case "Dragon":
                result = ActorType.Dragon;
                return true;
            case "Shulker":
                result = ActorType.Shulker;
                return true;
            case "Endermite":
                result = ActorType.Endermite;
                return true;
            case "Agent":
                result = ActorType.Agent;
                return true;
            case "Vindicator":
                result = ActorType.Vindicator;
                return true;
            case "Phantom":
                result = ActorType.Phantom;
                return true;
            case "IllagerBeast":
                result = ActorType.IllagerBeast;
                return true;
            case "ArmorStand":
                result = ActorType.ArmorStand;
                return true;
            case "TripodCamera":
                result = ActorType.TripodCamera;
                return true;
            case "Player":
                result = ActorType.Player;
                return true;
            case "ItemEntity":
                result = ActorType.ItemEntity;
                return true;
            case "PrimedTnt":
                result = ActorType.PrimedTnt;
                return true;
            case "FallingBlock":
                result = ActorType.FallingBlock;
                return true;
            case "MovingBlock":
                result = ActorType.MovingBlock;
                return true;
            case "ExperiencePotion":
                result = ActorType.ExperiencePotion;
                return true;
            case "Experience":
                result = ActorType.Experience;
                return true;
            case "EyeOfEnder":
                result = ActorType.EyeOfEnder;
                return true;
            case "EnderCrystal":
                result = ActorType.EnderCrystal;
                return true;
            case "FireworksRocket":
                result = ActorType.FireworksRocket;
                return true;
            case "Trident":
                result = ActorType.Trident;
                return true;
            case "Turtle":
                result = ActorType.Turtle;
                return true;
            case "Cat":
                result = ActorType.Cat;
                return true;
            case "ShulkerBullet":
                result = ActorType.ShulkerBullet;
                return true;
            case "FishingHook":
                result = ActorType.FishingHook;
                return true;
            case "Chalkboard":
                result = ActorType.Chalkboard;
                return true;
            case "DragonFireball":
                result = ActorType.DragonFireball;
                return true;
            case "Arrow":
                result = ActorType.Arrow;
                return true;
            case "Snowball":
                result = ActorType.Snowball;
                return true;
            case "ThrownEgg":
                result = ActorType.ThrownEgg;
                return true;
            case "Painting":
                result = ActorType.Painting;
                return true;
            case "LargeFireball":
                result = ActorType.LargeFireball;
                return true;
            case "ThrownPotion":
                result = ActorType.ThrownPotion;
                return true;
            case "Enderpearl":
                result = ActorType.Enderpearl;
                return true;
            case "LeashKnot":
                result = ActorType.LeashKnot;
                return true;
            case "WitherSkull":
                result = ActorType.WitherSkull;
                return true;
            case "BoatRideable":
                result = ActorType.BoatRideable;
                return true;
            case "WitherSkullDangerous":
                result = ActorType.WitherSkullDangerous;
                return true;
            case "LightningBolt":
                result = ActorType.LightningBolt;
                return true;
            case "SmallFireball":
                result = ActorType.SmallFireball;
                return true;
            case "AreaEffectCloud":
                result = ActorType.AreaEffectCloud;
                return true;
            case "LingeringPotion":
                result = ActorType.LingeringPotion;
                return true;
            case "LlamaSpit":
                result = ActorType.LlamaSpit;
                return true;
            case "EvocationFang":
                result = ActorType.EvocationFang;
                return true;
            case "EvocationIllager":
                result = ActorType.EvocationIllager;
                return true;
            case "Vex":
                result = ActorType.Vex;
                return true;
            case "MinecartRideable":
                result = ActorType.MinecartRideable;
                return true;
            case "MinecartHopper":
                result = ActorType.MinecartHopper;
                return true;
            case "MinecartTNT":
                result = ActorType.MinecartTNT;
                return true;
            case "MinecartChest":
                result = ActorType.MinecartChest;
                return true;
            case "MinecartFurnace":
                result = ActorType.MinecartFurnace;
                return true;
            case "MinecartCommandBlock":
                result = ActorType.MinecartCommandBlock;
                return true;
            case "IceBomb":
                result = ActorType.IceBomb;
                return true;
            case "Balloon":
                result = ActorType.Balloon;
                return true;
            case "Pufferfish":
                result = ActorType.Pufferfish;
                return true;
            case "Salmon":
                result = ActorType.Salmon;
                return true;
            case "Drowned":
                result = ActorType.Drowned;
                return true;
            case "Tropicalfish":
                result = ActorType.Tropicalfish;
                return true;
            case "Fish":
                result = ActorType.Fish;
                return true;
            case "Panda":
                result = ActorType.Panda;
                return true;
            case "Pillager":
                result = ActorType.Pillager;
                return true;
            case "VillagerV2":
                result = ActorType.VillagerV2;
                return true;
            case "ZombieVillagerV2":
                result = ActorType.ZombieVillagerV2;
                return true;
            case "Shield":
                result = ActorType.Shield;
                return true;
            case "WanderingTrader":
                result = ActorType.WanderingTrader;
                return true;
            case "Lectern":
                result = ActorType.Lectern;
                return true;
            case "ElderGuardianGhost":
                result = ActorType.ElderGuardianGhost;
                return true;
            case "Fox":
                result = ActorType.Fox;
                return true;
            case "Bee":
                result = ActorType.Bee;
                return true;
            case "Piglin":
                result = ActorType.Piglin;
                return true;
            case "Hoglin":
                result = ActorType.Hoglin;
                return true;
            case "Strider":
                result = ActorType.Strider;
                return true;
            case "Zoglin":
                result = ActorType.Zoglin;
                return true;
            case "PiglinBrute":
                result = ActorType.PiglinBrute;
                return true;
            case "Goat":
                result = ActorType.Goat;
                return true;
            case "GlowSquid":
                result = ActorType.GlowSquid;
                return true;
            case "Axolotl":
                result = ActorType.Axolotl;
                return true;
            case "Warden":
                result = ActorType.Warden;
                return true;
            case "Frog":
                result = ActorType.Frog;
                return true;
            case "Tadpole":
                result = ActorType.Tadpole;
                return true;
            case "Allay":
                result = ActorType.Allay;
                return true;
            case "ChestBoatRideable":
                result = ActorType.ChestBoatRideable;
                return true;
            case "TraderLlama":
                result = ActorType.TraderLlama;
                return true;
            case "Camel":
                result = ActorType.Camel;
                return true;
            case "Sniffer":
                result = ActorType.Sniffer;
                return true;
            case "Breeze":
                result = ActorType.Breeze;
                return true;
            case "BreezeWindChargeProjectile":
                result = ActorType.BreezeWindChargeProjectile;
                return true;
            case "Armadillo":
                result = ActorType.Armadillo;
                return true;
            case "WindChargeProjectile":
                result = ActorType.WindChargeProjectile;
                return true;
            case "Bogged":
                result = ActorType.Bogged;
                return true;
            case "OminousItemSpawner":
                result = ActorType.OminousItemSpawner;
                return true;
            case "Creaking":
                result = ActorType.Creaking;
                return true;
            case "HappyGhast":
                result = ActorType.HappyGhast;
                return true;
            case "CopperGolem":
                result = ActorType.CopperGolem;
                return true;
            case "Nautilus":
                result = ActorType.Nautilus;
                return true;
            case "ZombieNautilus":
                result = ActorType.ZombieNautilus;
                return true;
            case "Parched":
                result = ActorType.Parched;
                return true;
            case "CamelHusk":
                result = ActorType.CamelHusk;
                return true;
            case "SulfurCube":
                result = ActorType.SulfurCube;
                return true;
            case "Cushion":
                result = ActorType.Cushion;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

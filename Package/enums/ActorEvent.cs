using System;

namespace BedrockProtocol.Enums;

public enum ActorEvent {
    NONE = 0,
    JUMP = 1,
    HURT = 2,
    DEATH = 3,
    START_ATTACKING = 4,
    STOP_ATTACKING = 5,
    TAMING_FAILED = 6,
    TAMING_SUCCEEDED = 7,
    SHAKE_WETNESS = 8,
    EAT_GRASS = 10,
    FISHHOOK_BUBBLE = 11,
    FISHHOOK_FISHPOS = 12,
    FISHHOOK_HOOKTIME = 13,
    FISHHOOK_TEASE = 14,
    SQUID_FLEEING = 15,
    ZOMBIE_CONVERTING = 16,
    PLAY_AMBIENT = 17,
    SPAWN_ALIVE = 18,
    START_OFFER_FLOWER = 19,
    STOP_OFFER_FLOWER = 20,
    LOVE_HEARTS = 21,
    VILLAGER_ANGRY = 22,
    VILLAGER_HAPPY = 23,
    WITCH_HAT_MAGIC = 24,
    FIREWORKS_EXPLODE = 25,
    IN_LOVE_HEARTS = 26,
    SILVERFISH_MERGE_ANIM = 27,
    GUARDIAN_ATTACK_SOUND = 28,
    DRINK_POTION = 29,
    THROW_POTION = 30,
    PRIME_TNTCART = 31,
    PRIME_CREEPER = 32,
    AIR_SUPPLY = 33,
    DEPRECATED_ADD_PLAYER_LEVELS = 34,
    GUARDIAN_MINING_FATIGUE = 35,
    AGENT_SWING_ARM = 36,
    DRAGON_START_DEATH_ANIM = 37,
    GROUND_DUST = 38,
    SHAKE = 39,
    FEED = 57,
    BABY_AGE = 60,
    INSTANT_DEATH = 61,
    NOTIFY_TRADE = 62,
    LEASH_DESTROYED = 63,
    CARAVAN_UPDATED = 64,
    TALISMAN_ACTIVATE = 65,
    DEPRECATED_UPDATE_STRUCTURE_FEATURE = 66,
    PLAYER_SPAWNED_MOB = 67,
    PUKE = 68,
    UPDATE_STACK_SIZE = 69,
    START_SWIMMING = 70,
    BALLOON_POP = 71,
    TREASURE_HUNT = 72,
    SUMMON_AGENT = 73,
    FINISHED_CHARGING_ITEM = 74,
    ACTOR_GROW_UP = 76,
    VIBRATION_DETECTED = 77,
    DRINK_MILK = 78,
    SHAKE_WETNESS_STOP = 79,
    KINETIC_DAMAGE_DEALT = 80,
    HURT_WITHOUT_RECEIVING_DAMAGE = 81,
}

public static class ActorEventExtensions {
    public static string ToProtoString(this ActorEvent value) => value.ToProtocolString();

    public static string ToProtocolString(this ActorEvent value) {
        return value switch {
            ActorEvent.NONE => "NONE",
            ActorEvent.JUMP => "JUMP",
            ActorEvent.HURT => "HURT",
            ActorEvent.DEATH => "DEATH",
            ActorEvent.START_ATTACKING => "START_ATTACKING",
            ActorEvent.STOP_ATTACKING => "STOP_ATTACKING",
            ActorEvent.TAMING_FAILED => "TAMING_FAILED",
            ActorEvent.TAMING_SUCCEEDED => "TAMING_SUCCEEDED",
            ActorEvent.SHAKE_WETNESS => "SHAKE_WETNESS",
            ActorEvent.EAT_GRASS => "EAT_GRASS",
            ActorEvent.FISHHOOK_BUBBLE => "FISHHOOK_BUBBLE",
            ActorEvent.FISHHOOK_FISHPOS => "FISHHOOK_FISHPOS",
            ActorEvent.FISHHOOK_HOOKTIME => "FISHHOOK_HOOKTIME",
            ActorEvent.FISHHOOK_TEASE => "FISHHOOK_TEASE",
            ActorEvent.SQUID_FLEEING => "SQUID_FLEEING",
            ActorEvent.ZOMBIE_CONVERTING => "ZOMBIE_CONVERTING",
            ActorEvent.PLAY_AMBIENT => "PLAY_AMBIENT",
            ActorEvent.SPAWN_ALIVE => "SPAWN_ALIVE",
            ActorEvent.START_OFFER_FLOWER => "START_OFFER_FLOWER",
            ActorEvent.STOP_OFFER_FLOWER => "STOP_OFFER_FLOWER",
            ActorEvent.LOVE_HEARTS => "LOVE_HEARTS",
            ActorEvent.VILLAGER_ANGRY => "VILLAGER_ANGRY",
            ActorEvent.VILLAGER_HAPPY => "VILLAGER_HAPPY",
            ActorEvent.WITCH_HAT_MAGIC => "WITCH_HAT_MAGIC",
            ActorEvent.FIREWORKS_EXPLODE => "FIREWORKS_EXPLODE",
            ActorEvent.IN_LOVE_HEARTS => "IN_LOVE_HEARTS",
            ActorEvent.SILVERFISH_MERGE_ANIM => "SILVERFISH_MERGE_ANIM",
            ActorEvent.GUARDIAN_ATTACK_SOUND => "GUARDIAN_ATTACK_SOUND",
            ActorEvent.DRINK_POTION => "DRINK_POTION",
            ActorEvent.THROW_POTION => "THROW_POTION",
            ActorEvent.PRIME_TNTCART => "PRIME_TNTCART",
            ActorEvent.PRIME_CREEPER => "PRIME_CREEPER",
            ActorEvent.AIR_SUPPLY => "AIR_SUPPLY",
            ActorEvent.DEPRECATED_ADD_PLAYER_LEVELS => "DEPRECATED_ADD_PLAYER_LEVELS",
            ActorEvent.GUARDIAN_MINING_FATIGUE => "GUARDIAN_MINING_FATIGUE",
            ActorEvent.AGENT_SWING_ARM => "AGENT_SWING_ARM",
            ActorEvent.DRAGON_START_DEATH_ANIM => "DRAGON_START_DEATH_ANIM",
            ActorEvent.GROUND_DUST => "GROUND_DUST",
            ActorEvent.SHAKE => "SHAKE",
            ActorEvent.FEED => "FEED",
            ActorEvent.BABY_AGE => "BABY_AGE",
            ActorEvent.INSTANT_DEATH => "INSTANT_DEATH",
            ActorEvent.NOTIFY_TRADE => "NOTIFY_TRADE",
            ActorEvent.LEASH_DESTROYED => "LEASH_DESTROYED",
            ActorEvent.CARAVAN_UPDATED => "CARAVAN_UPDATED",
            ActorEvent.TALISMAN_ACTIVATE => "TALISMAN_ACTIVATE",
            ActorEvent.DEPRECATED_UPDATE_STRUCTURE_FEATURE => "DEPRECATED_UPDATE_STRUCTURE_FEATURE",
            ActorEvent.PLAYER_SPAWNED_MOB => "PLAYER_SPAWNED_MOB",
            ActorEvent.PUKE => "PUKE",
            ActorEvent.UPDATE_STACK_SIZE => "UPDATE_STACK_SIZE",
            ActorEvent.START_SWIMMING => "START_SWIMMING",
            ActorEvent.BALLOON_POP => "BALLOON_POP",
            ActorEvent.TREASURE_HUNT => "TREASURE_HUNT",
            ActorEvent.SUMMON_AGENT => "SUMMON_AGENT",
            ActorEvent.FINISHED_CHARGING_ITEM => "FINISHED_CHARGING_ITEM",
            ActorEvent.ACTOR_GROW_UP => "ACTOR_GROW_UP",
            ActorEvent.VIBRATION_DETECTED => "VIBRATION_DETECTED",
            ActorEvent.DRINK_MILK => "DRINK_MILK",
            ActorEvent.SHAKE_WETNESS_STOP => "SHAKE_WETNESS_STOP",
            ActorEvent.KINETIC_DAMAGE_DEALT => "KINETIC_DAMAGE_DEALT",
            ActorEvent.HURT_WITHOUT_RECEIVING_DAMAGE => "HURT_WITHOUT_RECEIVING_DAMAGE",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ActorEvent value.")
        };
    }

    public static ActorEvent FromProtocolString(string value) {
        return value switch {
            "NONE" => ActorEvent.NONE,
            "JUMP" => ActorEvent.JUMP,
            "HURT" => ActorEvent.HURT,
            "DEATH" => ActorEvent.DEATH,
            "START_ATTACKING" => ActorEvent.START_ATTACKING,
            "STOP_ATTACKING" => ActorEvent.STOP_ATTACKING,
            "TAMING_FAILED" => ActorEvent.TAMING_FAILED,
            "TAMING_SUCCEEDED" => ActorEvent.TAMING_SUCCEEDED,
            "SHAKE_WETNESS" => ActorEvent.SHAKE_WETNESS,
            "EAT_GRASS" => ActorEvent.EAT_GRASS,
            "FISHHOOK_BUBBLE" => ActorEvent.FISHHOOK_BUBBLE,
            "FISHHOOK_FISHPOS" => ActorEvent.FISHHOOK_FISHPOS,
            "FISHHOOK_HOOKTIME" => ActorEvent.FISHHOOK_HOOKTIME,
            "FISHHOOK_TEASE" => ActorEvent.FISHHOOK_TEASE,
            "SQUID_FLEEING" => ActorEvent.SQUID_FLEEING,
            "ZOMBIE_CONVERTING" => ActorEvent.ZOMBIE_CONVERTING,
            "PLAY_AMBIENT" => ActorEvent.PLAY_AMBIENT,
            "SPAWN_ALIVE" => ActorEvent.SPAWN_ALIVE,
            "START_OFFER_FLOWER" => ActorEvent.START_OFFER_FLOWER,
            "STOP_OFFER_FLOWER" => ActorEvent.STOP_OFFER_FLOWER,
            "LOVE_HEARTS" => ActorEvent.LOVE_HEARTS,
            "VILLAGER_ANGRY" => ActorEvent.VILLAGER_ANGRY,
            "VILLAGER_HAPPY" => ActorEvent.VILLAGER_HAPPY,
            "WITCH_HAT_MAGIC" => ActorEvent.WITCH_HAT_MAGIC,
            "FIREWORKS_EXPLODE" => ActorEvent.FIREWORKS_EXPLODE,
            "IN_LOVE_HEARTS" => ActorEvent.IN_LOVE_HEARTS,
            "SILVERFISH_MERGE_ANIM" => ActorEvent.SILVERFISH_MERGE_ANIM,
            "GUARDIAN_ATTACK_SOUND" => ActorEvent.GUARDIAN_ATTACK_SOUND,
            "DRINK_POTION" => ActorEvent.DRINK_POTION,
            "THROW_POTION" => ActorEvent.THROW_POTION,
            "PRIME_TNTCART" => ActorEvent.PRIME_TNTCART,
            "PRIME_CREEPER" => ActorEvent.PRIME_CREEPER,
            "AIR_SUPPLY" => ActorEvent.AIR_SUPPLY,
            "DEPRECATED_ADD_PLAYER_LEVELS" => ActorEvent.DEPRECATED_ADD_PLAYER_LEVELS,
            "GUARDIAN_MINING_FATIGUE" => ActorEvent.GUARDIAN_MINING_FATIGUE,
            "AGENT_SWING_ARM" => ActorEvent.AGENT_SWING_ARM,
            "DRAGON_START_DEATH_ANIM" => ActorEvent.DRAGON_START_DEATH_ANIM,
            "GROUND_DUST" => ActorEvent.GROUND_DUST,
            "SHAKE" => ActorEvent.SHAKE,
            "FEED" => ActorEvent.FEED,
            "BABY_AGE" => ActorEvent.BABY_AGE,
            "INSTANT_DEATH" => ActorEvent.INSTANT_DEATH,
            "NOTIFY_TRADE" => ActorEvent.NOTIFY_TRADE,
            "LEASH_DESTROYED" => ActorEvent.LEASH_DESTROYED,
            "CARAVAN_UPDATED" => ActorEvent.CARAVAN_UPDATED,
            "TALISMAN_ACTIVATE" => ActorEvent.TALISMAN_ACTIVATE,
            "DEPRECATED_UPDATE_STRUCTURE_FEATURE" => ActorEvent.DEPRECATED_UPDATE_STRUCTURE_FEATURE,
            "PLAYER_SPAWNED_MOB" => ActorEvent.PLAYER_SPAWNED_MOB,
            "PUKE" => ActorEvent.PUKE,
            "UPDATE_STACK_SIZE" => ActorEvent.UPDATE_STACK_SIZE,
            "START_SWIMMING" => ActorEvent.START_SWIMMING,
            "BALLOON_POP" => ActorEvent.BALLOON_POP,
            "TREASURE_HUNT" => ActorEvent.TREASURE_HUNT,
            "SUMMON_AGENT" => ActorEvent.SUMMON_AGENT,
            "FINISHED_CHARGING_ITEM" => ActorEvent.FINISHED_CHARGING_ITEM,
            "ACTOR_GROW_UP" => ActorEvent.ACTOR_GROW_UP,
            "VIBRATION_DETECTED" => ActorEvent.VIBRATION_DETECTED,
            "DRINK_MILK" => ActorEvent.DRINK_MILK,
            "SHAKE_WETNESS_STOP" => ActorEvent.SHAKE_WETNESS_STOP,
            "KINETIC_DAMAGE_DEALT" => ActorEvent.KINETIC_DAMAGE_DEALT,
            "HURT_WITHOUT_RECEIVING_DAMAGE" => ActorEvent.HURT_WITHOUT_RECEIVING_DAMAGE,
            _ => throw new ArgumentException($"Unknown ActorEvent protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ActorEvent result) {
        switch (value) {
            case "NONE":
                result = ActorEvent.NONE;
                return true;
            case "JUMP":
                result = ActorEvent.JUMP;
                return true;
            case "HURT":
                result = ActorEvent.HURT;
                return true;
            case "DEATH":
                result = ActorEvent.DEATH;
                return true;
            case "START_ATTACKING":
                result = ActorEvent.START_ATTACKING;
                return true;
            case "STOP_ATTACKING":
                result = ActorEvent.STOP_ATTACKING;
                return true;
            case "TAMING_FAILED":
                result = ActorEvent.TAMING_FAILED;
                return true;
            case "TAMING_SUCCEEDED":
                result = ActorEvent.TAMING_SUCCEEDED;
                return true;
            case "SHAKE_WETNESS":
                result = ActorEvent.SHAKE_WETNESS;
                return true;
            case "EAT_GRASS":
                result = ActorEvent.EAT_GRASS;
                return true;
            case "FISHHOOK_BUBBLE":
                result = ActorEvent.FISHHOOK_BUBBLE;
                return true;
            case "FISHHOOK_FISHPOS":
                result = ActorEvent.FISHHOOK_FISHPOS;
                return true;
            case "FISHHOOK_HOOKTIME":
                result = ActorEvent.FISHHOOK_HOOKTIME;
                return true;
            case "FISHHOOK_TEASE":
                result = ActorEvent.FISHHOOK_TEASE;
                return true;
            case "SQUID_FLEEING":
                result = ActorEvent.SQUID_FLEEING;
                return true;
            case "ZOMBIE_CONVERTING":
                result = ActorEvent.ZOMBIE_CONVERTING;
                return true;
            case "PLAY_AMBIENT":
                result = ActorEvent.PLAY_AMBIENT;
                return true;
            case "SPAWN_ALIVE":
                result = ActorEvent.SPAWN_ALIVE;
                return true;
            case "START_OFFER_FLOWER":
                result = ActorEvent.START_OFFER_FLOWER;
                return true;
            case "STOP_OFFER_FLOWER":
                result = ActorEvent.STOP_OFFER_FLOWER;
                return true;
            case "LOVE_HEARTS":
                result = ActorEvent.LOVE_HEARTS;
                return true;
            case "VILLAGER_ANGRY":
                result = ActorEvent.VILLAGER_ANGRY;
                return true;
            case "VILLAGER_HAPPY":
                result = ActorEvent.VILLAGER_HAPPY;
                return true;
            case "WITCH_HAT_MAGIC":
                result = ActorEvent.WITCH_HAT_MAGIC;
                return true;
            case "FIREWORKS_EXPLODE":
                result = ActorEvent.FIREWORKS_EXPLODE;
                return true;
            case "IN_LOVE_HEARTS":
                result = ActorEvent.IN_LOVE_HEARTS;
                return true;
            case "SILVERFISH_MERGE_ANIM":
                result = ActorEvent.SILVERFISH_MERGE_ANIM;
                return true;
            case "GUARDIAN_ATTACK_SOUND":
                result = ActorEvent.GUARDIAN_ATTACK_SOUND;
                return true;
            case "DRINK_POTION":
                result = ActorEvent.DRINK_POTION;
                return true;
            case "THROW_POTION":
                result = ActorEvent.THROW_POTION;
                return true;
            case "PRIME_TNTCART":
                result = ActorEvent.PRIME_TNTCART;
                return true;
            case "PRIME_CREEPER":
                result = ActorEvent.PRIME_CREEPER;
                return true;
            case "AIR_SUPPLY":
                result = ActorEvent.AIR_SUPPLY;
                return true;
            case "DEPRECATED_ADD_PLAYER_LEVELS":
                result = ActorEvent.DEPRECATED_ADD_PLAYER_LEVELS;
                return true;
            case "GUARDIAN_MINING_FATIGUE":
                result = ActorEvent.GUARDIAN_MINING_FATIGUE;
                return true;
            case "AGENT_SWING_ARM":
                result = ActorEvent.AGENT_SWING_ARM;
                return true;
            case "DRAGON_START_DEATH_ANIM":
                result = ActorEvent.DRAGON_START_DEATH_ANIM;
                return true;
            case "GROUND_DUST":
                result = ActorEvent.GROUND_DUST;
                return true;
            case "SHAKE":
                result = ActorEvent.SHAKE;
                return true;
            case "FEED":
                result = ActorEvent.FEED;
                return true;
            case "BABY_AGE":
                result = ActorEvent.BABY_AGE;
                return true;
            case "INSTANT_DEATH":
                result = ActorEvent.INSTANT_DEATH;
                return true;
            case "NOTIFY_TRADE":
                result = ActorEvent.NOTIFY_TRADE;
                return true;
            case "LEASH_DESTROYED":
                result = ActorEvent.LEASH_DESTROYED;
                return true;
            case "CARAVAN_UPDATED":
                result = ActorEvent.CARAVAN_UPDATED;
                return true;
            case "TALISMAN_ACTIVATE":
                result = ActorEvent.TALISMAN_ACTIVATE;
                return true;
            case "DEPRECATED_UPDATE_STRUCTURE_FEATURE":
                result = ActorEvent.DEPRECATED_UPDATE_STRUCTURE_FEATURE;
                return true;
            case "PLAYER_SPAWNED_MOB":
                result = ActorEvent.PLAYER_SPAWNED_MOB;
                return true;
            case "PUKE":
                result = ActorEvent.PUKE;
                return true;
            case "UPDATE_STACK_SIZE":
                result = ActorEvent.UPDATE_STACK_SIZE;
                return true;
            case "START_SWIMMING":
                result = ActorEvent.START_SWIMMING;
                return true;
            case "BALLOON_POP":
                result = ActorEvent.BALLOON_POP;
                return true;
            case "TREASURE_HUNT":
                result = ActorEvent.TREASURE_HUNT;
                return true;
            case "SUMMON_AGENT":
                result = ActorEvent.SUMMON_AGENT;
                return true;
            case "FINISHED_CHARGING_ITEM":
                result = ActorEvent.FINISHED_CHARGING_ITEM;
                return true;
            case "ACTOR_GROW_UP":
                result = ActorEvent.ACTOR_GROW_UP;
                return true;
            case "VIBRATION_DETECTED":
                result = ActorEvent.VIBRATION_DETECTED;
                return true;
            case "DRINK_MILK":
                result = ActorEvent.DRINK_MILK;
                return true;
            case "SHAKE_WETNESS_STOP":
                result = ActorEvent.SHAKE_WETNESS_STOP;
                return true;
            case "KINETIC_DAMAGE_DEALT":
                result = ActorEvent.KINETIC_DAMAGE_DEALT;
                return true;
            case "HURT_WITHOUT_RECEIVING_DAMAGE":
                result = ActorEvent.HURT_WITHOUT_RECEIVING_DAMAGE;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

using System;

namespace BedrockProtocol.Enums;

public enum ContainerType {
    NONE = -9,
    INVENTORY = -1,
    CONTAINER = 0,
    WORKBENCH = 1,
    FURNACE = 2,
    ENCHANTMENT = 3,
    BREWING_STAND = 4,
    ANVIL = 5,
    DISPENSER = 6,
    DROPPER = 7,
    HOPPER = 8,
    CAULDRON = 9,
    MINECART_CHEST = 10,
    MINECART_HOPPER = 11,
    HORSE = 12,
    BEACON = 13,
    STRUCTURE_EDITOR = 14,
    TRADE = 15,
    COMMAND_BLOCK = 16,
    JUKEBOX = 17,
    ARMOR = 18,
    HAND = 19,
    COMPOUND_CREATOR = 20,
    ELEMENT_CONSTRUCTOR = 21,
    MATERIAL_REDUCER = 22,
    LAB_TABLE = 23,
    LOOM = 24,
    LECTERN = 25,
    GRINDSTONE = 26,
    BLAST_FURNACE = 27,
    SMOKER = 28,
    STONECUTTER = 29,
    CARTOGRAPHY = 30,
    HUD = 31,
    JIGSAW_EDITOR = 32,
    SMITHING_TABLE = 33,
    CHEST_BOAT = 34,
    DECORATED_POT = 35,
    CRAFTER = 36,
}

public static class ContainerTypeExtensions {
    public static string ToProtoString(this ContainerType value) => value.ToProtocolString();

    public static string ToProtocolString(this ContainerType value) {
        return value switch {
            ContainerType.NONE => "NONE",
            ContainerType.INVENTORY => "INVENTORY",
            ContainerType.CONTAINER => "CONTAINER",
            ContainerType.WORKBENCH => "WORKBENCH",
            ContainerType.FURNACE => "FURNACE",
            ContainerType.ENCHANTMENT => "ENCHANTMENT",
            ContainerType.BREWING_STAND => "BREWING_STAND",
            ContainerType.ANVIL => "ANVIL",
            ContainerType.DISPENSER => "DISPENSER",
            ContainerType.DROPPER => "DROPPER",
            ContainerType.HOPPER => "HOPPER",
            ContainerType.CAULDRON => "CAULDRON",
            ContainerType.MINECART_CHEST => "MINECART_CHEST",
            ContainerType.MINECART_HOPPER => "MINECART_HOPPER",
            ContainerType.HORSE => "HORSE",
            ContainerType.BEACON => "BEACON",
            ContainerType.STRUCTURE_EDITOR => "STRUCTURE_EDITOR",
            ContainerType.TRADE => "TRADE",
            ContainerType.COMMAND_BLOCK => "COMMAND_BLOCK",
            ContainerType.JUKEBOX => "JUKEBOX",
            ContainerType.ARMOR => "ARMOR",
            ContainerType.HAND => "HAND",
            ContainerType.COMPOUND_CREATOR => "COMPOUND_CREATOR",
            ContainerType.ELEMENT_CONSTRUCTOR => "ELEMENT_CONSTRUCTOR",
            ContainerType.MATERIAL_REDUCER => "MATERIAL_REDUCER",
            ContainerType.LAB_TABLE => "LAB_TABLE",
            ContainerType.LOOM => "LOOM",
            ContainerType.LECTERN => "LECTERN",
            ContainerType.GRINDSTONE => "GRINDSTONE",
            ContainerType.BLAST_FURNACE => "BLAST_FURNACE",
            ContainerType.SMOKER => "SMOKER",
            ContainerType.STONECUTTER => "STONECUTTER",
            ContainerType.CARTOGRAPHY => "CARTOGRAPHY",
            ContainerType.HUD => "HUD",
            ContainerType.JIGSAW_EDITOR => "JIGSAW_EDITOR",
            ContainerType.SMITHING_TABLE => "SMITHING_TABLE",
            ContainerType.CHEST_BOAT => "CHEST_BOAT",
            ContainerType.DECORATED_POT => "DECORATED_POT",
            ContainerType.CRAFTER => "CRAFTER",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ContainerType value.")
        };
    }

    public static ContainerType FromProtocolString(string value) {
        return value switch {
            "NONE" => ContainerType.NONE,
            "INVENTORY" => ContainerType.INVENTORY,
            "CONTAINER" => ContainerType.CONTAINER,
            "WORKBENCH" => ContainerType.WORKBENCH,
            "FURNACE" => ContainerType.FURNACE,
            "ENCHANTMENT" => ContainerType.ENCHANTMENT,
            "BREWING_STAND" => ContainerType.BREWING_STAND,
            "ANVIL" => ContainerType.ANVIL,
            "DISPENSER" => ContainerType.DISPENSER,
            "DROPPER" => ContainerType.DROPPER,
            "HOPPER" => ContainerType.HOPPER,
            "CAULDRON" => ContainerType.CAULDRON,
            "MINECART_CHEST" => ContainerType.MINECART_CHEST,
            "MINECART_HOPPER" => ContainerType.MINECART_HOPPER,
            "HORSE" => ContainerType.HORSE,
            "BEACON" => ContainerType.BEACON,
            "STRUCTURE_EDITOR" => ContainerType.STRUCTURE_EDITOR,
            "TRADE" => ContainerType.TRADE,
            "COMMAND_BLOCK" => ContainerType.COMMAND_BLOCK,
            "JUKEBOX" => ContainerType.JUKEBOX,
            "ARMOR" => ContainerType.ARMOR,
            "HAND" => ContainerType.HAND,
            "COMPOUND_CREATOR" => ContainerType.COMPOUND_CREATOR,
            "ELEMENT_CONSTRUCTOR" => ContainerType.ELEMENT_CONSTRUCTOR,
            "MATERIAL_REDUCER" => ContainerType.MATERIAL_REDUCER,
            "LAB_TABLE" => ContainerType.LAB_TABLE,
            "LOOM" => ContainerType.LOOM,
            "LECTERN" => ContainerType.LECTERN,
            "GRINDSTONE" => ContainerType.GRINDSTONE,
            "BLAST_FURNACE" => ContainerType.BLAST_FURNACE,
            "SMOKER" => ContainerType.SMOKER,
            "STONECUTTER" => ContainerType.STONECUTTER,
            "CARTOGRAPHY" => ContainerType.CARTOGRAPHY,
            "HUD" => ContainerType.HUD,
            "JIGSAW_EDITOR" => ContainerType.JIGSAW_EDITOR,
            "SMITHING_TABLE" => ContainerType.SMITHING_TABLE,
            "CHEST_BOAT" => ContainerType.CHEST_BOAT,
            "DECORATED_POT" => ContainerType.DECORATED_POT,
            "CRAFTER" => ContainerType.CRAFTER,
            _ => throw new ArgumentException($"Unknown ContainerType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ContainerType result) {
        switch (value) {
            case "NONE":
                result = ContainerType.NONE;
                return true;
            case "INVENTORY":
                result = ContainerType.INVENTORY;
                return true;
            case "CONTAINER":
                result = ContainerType.CONTAINER;
                return true;
            case "WORKBENCH":
                result = ContainerType.WORKBENCH;
                return true;
            case "FURNACE":
                result = ContainerType.FURNACE;
                return true;
            case "ENCHANTMENT":
                result = ContainerType.ENCHANTMENT;
                return true;
            case "BREWING_STAND":
                result = ContainerType.BREWING_STAND;
                return true;
            case "ANVIL":
                result = ContainerType.ANVIL;
                return true;
            case "DISPENSER":
                result = ContainerType.DISPENSER;
                return true;
            case "DROPPER":
                result = ContainerType.DROPPER;
                return true;
            case "HOPPER":
                result = ContainerType.HOPPER;
                return true;
            case "CAULDRON":
                result = ContainerType.CAULDRON;
                return true;
            case "MINECART_CHEST":
                result = ContainerType.MINECART_CHEST;
                return true;
            case "MINECART_HOPPER":
                result = ContainerType.MINECART_HOPPER;
                return true;
            case "HORSE":
                result = ContainerType.HORSE;
                return true;
            case "BEACON":
                result = ContainerType.BEACON;
                return true;
            case "STRUCTURE_EDITOR":
                result = ContainerType.STRUCTURE_EDITOR;
                return true;
            case "TRADE":
                result = ContainerType.TRADE;
                return true;
            case "COMMAND_BLOCK":
                result = ContainerType.COMMAND_BLOCK;
                return true;
            case "JUKEBOX":
                result = ContainerType.JUKEBOX;
                return true;
            case "ARMOR":
                result = ContainerType.ARMOR;
                return true;
            case "HAND":
                result = ContainerType.HAND;
                return true;
            case "COMPOUND_CREATOR":
                result = ContainerType.COMPOUND_CREATOR;
                return true;
            case "ELEMENT_CONSTRUCTOR":
                result = ContainerType.ELEMENT_CONSTRUCTOR;
                return true;
            case "MATERIAL_REDUCER":
                result = ContainerType.MATERIAL_REDUCER;
                return true;
            case "LAB_TABLE":
                result = ContainerType.LAB_TABLE;
                return true;
            case "LOOM":
                result = ContainerType.LOOM;
                return true;
            case "LECTERN":
                result = ContainerType.LECTERN;
                return true;
            case "GRINDSTONE":
                result = ContainerType.GRINDSTONE;
                return true;
            case "BLAST_FURNACE":
                result = ContainerType.BLAST_FURNACE;
                return true;
            case "SMOKER":
                result = ContainerType.SMOKER;
                return true;
            case "STONECUTTER":
                result = ContainerType.STONECUTTER;
                return true;
            case "CARTOGRAPHY":
                result = ContainerType.CARTOGRAPHY;
                return true;
            case "HUD":
                result = ContainerType.HUD;
                return true;
            case "JIGSAW_EDITOR":
                result = ContainerType.JIGSAW_EDITOR;
                return true;
            case "SMITHING_TABLE":
                result = ContainerType.SMITHING_TABLE;
                return true;
            case "CHEST_BOAT":
                result = ContainerType.CHEST_BOAT;
                return true;
            case "DECORATED_POT":
                result = ContainerType.DECORATED_POT;
                return true;
            case "CRAFTER":
                result = ContainerType.CRAFTER;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

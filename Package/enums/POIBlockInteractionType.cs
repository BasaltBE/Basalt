#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum POIBlockInteractionType {
    None = 0,
    Extend = 1,
    Clone = 2,
    Lock = 3,
    Create = 4,
    CreateLocator = 5,
    Rename = 6,
    ItemPlaced = 7,
    ItemRemoved = 8,
    Cooking = 9,
    Dousing = 10,
    Lighting = 11,
    Haystack = 12,
    Filled = 13,
    Emptied = 14,
    AddDye = 15,
    DyeItem = 16,
    ClearItem = 17,
    EnchantArrow = 18,
    CompostItemPlaced = 19,
    RecoveredBonemeal = 20,
    BookPlaced = 21,
    BookOpened = 22,
    Disenchant = 23,
    Repair = 24,
    DisenchantAndRepair = 25,
}

public static class POIBlockInteractionTypeExtensions {
    public static string ToProtoString(this POIBlockInteractionType value) => value.ToProtocolString();

    public static string ToProtocolString(this POIBlockInteractionType value) {
        return value switch {
            POIBlockInteractionType.None => "None",
            POIBlockInteractionType.Extend => "Extend",
            POIBlockInteractionType.Clone => "Clone",
            POIBlockInteractionType.Lock => "Lock",
            POIBlockInteractionType.Create => "Create",
            POIBlockInteractionType.CreateLocator => "CreateLocator",
            POIBlockInteractionType.Rename => "Rename",
            POIBlockInteractionType.ItemPlaced => "ItemPlaced",
            POIBlockInteractionType.ItemRemoved => "ItemRemoved",
            POIBlockInteractionType.Cooking => "Cooking",
            POIBlockInteractionType.Dousing => "Dousing",
            POIBlockInteractionType.Lighting => "Lighting",
            POIBlockInteractionType.Haystack => "Haystack",
            POIBlockInteractionType.Filled => "Filled",
            POIBlockInteractionType.Emptied => "Emptied",
            POIBlockInteractionType.AddDye => "AddDye",
            POIBlockInteractionType.DyeItem => "DyeItem",
            POIBlockInteractionType.ClearItem => "ClearItem",
            POIBlockInteractionType.EnchantArrow => "EnchantArrow",
            POIBlockInteractionType.CompostItemPlaced => "CompostItemPlaced",
            POIBlockInteractionType.RecoveredBonemeal => "RecoveredBonemeal",
            POIBlockInteractionType.BookPlaced => "BookPlaced",
            POIBlockInteractionType.BookOpened => "BookOpened",
            POIBlockInteractionType.Disenchant => "Disenchant",
            POIBlockInteractionType.Repair => "Repair",
            POIBlockInteractionType.DisenchantAndRepair => "DisenchantAndRepair",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown POIBlockInteractionType value.")
        };
    }

    public static POIBlockInteractionType FromProtocolString(string value) {
        return value switch {
            "None" => POIBlockInteractionType.None,
            "Extend" => POIBlockInteractionType.Extend,
            "Clone" => POIBlockInteractionType.Clone,
            "Lock" => POIBlockInteractionType.Lock,
            "Create" => POIBlockInteractionType.Create,
            "CreateLocator" => POIBlockInteractionType.CreateLocator,
            "Rename" => POIBlockInteractionType.Rename,
            "ItemPlaced" => POIBlockInteractionType.ItemPlaced,
            "ItemRemoved" => POIBlockInteractionType.ItemRemoved,
            "Cooking" => POIBlockInteractionType.Cooking,
            "Dousing" => POIBlockInteractionType.Dousing,
            "Lighting" => POIBlockInteractionType.Lighting,
            "Haystack" => POIBlockInteractionType.Haystack,
            "Filled" => POIBlockInteractionType.Filled,
            "Emptied" => POIBlockInteractionType.Emptied,
            "AddDye" => POIBlockInteractionType.AddDye,
            "DyeItem" => POIBlockInteractionType.DyeItem,
            "ClearItem" => POIBlockInteractionType.ClearItem,
            "EnchantArrow" => POIBlockInteractionType.EnchantArrow,
            "CompostItemPlaced" => POIBlockInteractionType.CompostItemPlaced,
            "RecoveredBonemeal" => POIBlockInteractionType.RecoveredBonemeal,
            "BookPlaced" => POIBlockInteractionType.BookPlaced,
            "BookOpened" => POIBlockInteractionType.BookOpened,
            "Disenchant" => POIBlockInteractionType.Disenchant,
            "Repair" => POIBlockInteractionType.Repair,
            "DisenchantAndRepair" => POIBlockInteractionType.DisenchantAndRepair,
            _ => throw new ArgumentException($"Unknown POIBlockInteractionType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out POIBlockInteractionType result) {
        switch (value) {
            case "None":
                result = POIBlockInteractionType.None;
                return true;
            case "Extend":
                result = POIBlockInteractionType.Extend;
                return true;
            case "Clone":
                result = POIBlockInteractionType.Clone;
                return true;
            case "Lock":
                result = POIBlockInteractionType.Lock;
                return true;
            case "Create":
                result = POIBlockInteractionType.Create;
                return true;
            case "CreateLocator":
                result = POIBlockInteractionType.CreateLocator;
                return true;
            case "Rename":
                result = POIBlockInteractionType.Rename;
                return true;
            case "ItemPlaced":
                result = POIBlockInteractionType.ItemPlaced;
                return true;
            case "ItemRemoved":
                result = POIBlockInteractionType.ItemRemoved;
                return true;
            case "Cooking":
                result = POIBlockInteractionType.Cooking;
                return true;
            case "Dousing":
                result = POIBlockInteractionType.Dousing;
                return true;
            case "Lighting":
                result = POIBlockInteractionType.Lighting;
                return true;
            case "Haystack":
                result = POIBlockInteractionType.Haystack;
                return true;
            case "Filled":
                result = POIBlockInteractionType.Filled;
                return true;
            case "Emptied":
                result = POIBlockInteractionType.Emptied;
                return true;
            case "AddDye":
                result = POIBlockInteractionType.AddDye;
                return true;
            case "DyeItem":
                result = POIBlockInteractionType.DyeItem;
                return true;
            case "ClearItem":
                result = POIBlockInteractionType.ClearItem;
                return true;
            case "EnchantArrow":
                result = POIBlockInteractionType.EnchantArrow;
                return true;
            case "CompostItemPlaced":
                result = POIBlockInteractionType.CompostItemPlaced;
                return true;
            case "RecoveredBonemeal":
                result = POIBlockInteractionType.RecoveredBonemeal;
                return true;
            case "BookPlaced":
                result = POIBlockInteractionType.BookPlaced;
                return true;
            case "BookOpened":
                result = POIBlockInteractionType.BookOpened;
                return true;
            case "Disenchant":
                result = POIBlockInteractionType.Disenchant;
                return true;
            case "Repair":
                result = POIBlockInteractionType.Repair;
                return true;
            case "DisenchantAndRepair":
                result = POIBlockInteractionType.DisenchantAndRepair;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

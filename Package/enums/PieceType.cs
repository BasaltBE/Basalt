using System;

namespace BedrockProtocol.Enums;

public enum PieceType {
    Skeleton = 1,
    Body = 2,
    Skin = 3,
    Bottom = 4,
    Feet = 5,
    Dress = 6,
    Top = 7,
    High_Pants = 8,
    Hands = 9,
    Outerwear = 10,
    FacialHair = 11,
    Mouth = 12,
    Eyes = 13,
    Hair = 14,
    Hood = 15,
    Back = 16,
    FaceAccessory = 17,
    Head = 18,
    Legs = 19,
    LeftLeg = 20,
    RightLeg = 21,
    Arms = 22,
    LeftArm = 23,
    RightArm = 24,
    Capes = 25,
    ClassicSkin = 26,
    Emote = 27,
}

public static class PieceTypeExtensions {
    public static string ToProtoString(this PieceType value) => value.ToProtocolString();

    public static string ToProtocolString(this PieceType value) {
        return value switch {
            PieceType.Skeleton => "Skeleton",
            PieceType.Body => "Body",
            PieceType.Skin => "Skin",
            PieceType.Bottom => "Bottom",
            PieceType.Feet => "Feet",
            PieceType.Dress => "Dress",
            PieceType.Top => "Top",
            PieceType.High_Pants => "High_Pants",
            PieceType.Hands => "Hands",
            PieceType.Outerwear => "Outerwear",
            PieceType.FacialHair => "FacialHair",
            PieceType.Mouth => "Mouth",
            PieceType.Eyes => "Eyes",
            PieceType.Hair => "Hair",
            PieceType.Hood => "Hood",
            PieceType.Back => "Back",
            PieceType.FaceAccessory => "FaceAccessory",
            PieceType.Head => "Head",
            PieceType.Legs => "Legs",
            PieceType.LeftLeg => "LeftLeg",
            PieceType.RightLeg => "RightLeg",
            PieceType.Arms => "Arms",
            PieceType.LeftArm => "LeftArm",
            PieceType.RightArm => "RightArm",
            PieceType.Capes => "Capes",
            PieceType.ClassicSkin => "ClassicSkin",
            PieceType.Emote => "Emote",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PieceType value.")
        };
    }

    public static PieceType FromProtocolString(string value) {
        return value switch {
            "Skeleton" => PieceType.Skeleton,
            "Body" => PieceType.Body,
            "Skin" => PieceType.Skin,
            "Bottom" => PieceType.Bottom,
            "Feet" => PieceType.Feet,
            "Dress" => PieceType.Dress,
            "Top" => PieceType.Top,
            "High_Pants" => PieceType.High_Pants,
            "Hands" => PieceType.Hands,
            "Outerwear" => PieceType.Outerwear,
            "FacialHair" => PieceType.FacialHair,
            "Mouth" => PieceType.Mouth,
            "Eyes" => PieceType.Eyes,
            "Hair" => PieceType.Hair,
            "Hood" => PieceType.Hood,
            "Back" => PieceType.Back,
            "FaceAccessory" => PieceType.FaceAccessory,
            "Head" => PieceType.Head,
            "Legs" => PieceType.Legs,
            "LeftLeg" => PieceType.LeftLeg,
            "RightLeg" => PieceType.RightLeg,
            "Arms" => PieceType.Arms,
            "LeftArm" => PieceType.LeftArm,
            "RightArm" => PieceType.RightArm,
            "Capes" => PieceType.Capes,
            "ClassicSkin" => PieceType.ClassicSkin,
            "Emote" => PieceType.Emote,
            _ => throw new ArgumentException($"Unknown PieceType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PieceType result) {
        switch (value) {
            case "Skeleton":
                result = PieceType.Skeleton;
                return true;
            case "Body":
                result = PieceType.Body;
                return true;
            case "Skin":
                result = PieceType.Skin;
                return true;
            case "Bottom":
                result = PieceType.Bottom;
                return true;
            case "Feet":
                result = PieceType.Feet;
                return true;
            case "Dress":
                result = PieceType.Dress;
                return true;
            case "Top":
                result = PieceType.Top;
                return true;
            case "High_Pants":
                result = PieceType.High_Pants;
                return true;
            case "Hands":
                result = PieceType.Hands;
                return true;
            case "Outerwear":
                result = PieceType.Outerwear;
                return true;
            case "FacialHair":
                result = PieceType.FacialHair;
                return true;
            case "Mouth":
                result = PieceType.Mouth;
                return true;
            case "Eyes":
                result = PieceType.Eyes;
                return true;
            case "Hair":
                result = PieceType.Hair;
                return true;
            case "Hood":
                result = PieceType.Hood;
                return true;
            case "Back":
                result = PieceType.Back;
                return true;
            case "FaceAccessory":
                result = PieceType.FaceAccessory;
                return true;
            case "Head":
                result = PieceType.Head;
                return true;
            case "Legs":
                result = PieceType.Legs;
                return true;
            case "LeftLeg":
                result = PieceType.LeftLeg;
                return true;
            case "RightLeg":
                result = PieceType.RightLeg;
                return true;
            case "Arms":
                result = PieceType.Arms;
                return true;
            case "LeftArm":
                result = PieceType.LeftArm;
                return true;
            case "RightArm":
                result = PieceType.RightArm;
                return true;
            case "Capes":
                result = PieceType.Capes;
                return true;
            case "ClassicSkin":
                result = PieceType.ClassicSkin;
                return true;
            case "Emote":
                result = PieceType.Emote;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

using System;

namespace BedrockProtocol.Enums;

public enum MapDecorationType {
    MarkerWhite = 0,
    MarkerGreen = 1,
    MarkerRed = 2,
    MarkerBlue = 3,
    XWhite = 4,
    TriangleRed = 5,
    SquareWhite = 6,
    MarkerSign = 7,
    MarkerPink = 8,
    MarkerOrange = 9,
    MarkerYellow = 10,
    MarkerTeal = 11,
    TriangleGreen = 12,
    SmallSquareWhite = 13,
    Mansion = 14,
    Monument = 15,
    NoDraw = 16,
    VillageDesert = 17,
    VillagePlains = 18,
    VillageSavanna = 19,
    VillageSnowy = 20,
    VillageTaiga = 21,
    JungleTemple = 22,
    WitchHut = 23,
    TrialChambers = 24,
    Count = 25,
}

public static class MapDecorationTypeExtensions {
    public static string ToProtoString(this MapDecorationType value) => value.ToProtocolString();

    public static string ToProtocolString(this MapDecorationType value) {
        return value switch {
            MapDecorationType.MarkerWhite => "MarkerWhite",
            MapDecorationType.MarkerGreen => "MarkerGreen",
            MapDecorationType.MarkerRed => "MarkerRed",
            MapDecorationType.MarkerBlue => "MarkerBlue",
            MapDecorationType.XWhite => "XWhite",
            MapDecorationType.TriangleRed => "TriangleRed",
            MapDecorationType.SquareWhite => "SquareWhite",
            MapDecorationType.MarkerSign => "MarkerSign",
            MapDecorationType.MarkerPink => "MarkerPink",
            MapDecorationType.MarkerOrange => "MarkerOrange",
            MapDecorationType.MarkerYellow => "MarkerYellow",
            MapDecorationType.MarkerTeal => "MarkerTeal",
            MapDecorationType.TriangleGreen => "TriangleGreen",
            MapDecorationType.SmallSquareWhite => "SmallSquareWhite",
            MapDecorationType.Mansion => "Mansion",
            MapDecorationType.Monument => "Monument",
            MapDecorationType.NoDraw => "NoDraw",
            MapDecorationType.VillageDesert => "VillageDesert",
            MapDecorationType.VillagePlains => "VillagePlains",
            MapDecorationType.VillageSavanna => "VillageSavanna",
            MapDecorationType.VillageSnowy => "VillageSnowy",
            MapDecorationType.VillageTaiga => "VillageTaiga",
            MapDecorationType.JungleTemple => "JungleTemple",
            MapDecorationType.WitchHut => "WitchHut",
            MapDecorationType.TrialChambers => "TrialChambers",
            MapDecorationType.Count => "Count",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown MapDecorationType value.")
        };
    }

    public static MapDecorationType FromProtocolString(string value) {
        return value switch {
            "MarkerWhite" => MapDecorationType.MarkerWhite,
            "MarkerGreen" => MapDecorationType.MarkerGreen,
            "MarkerRed" => MapDecorationType.MarkerRed,
            "MarkerBlue" => MapDecorationType.MarkerBlue,
            "XWhite" => MapDecorationType.XWhite,
            "TriangleRed" => MapDecorationType.TriangleRed,
            "SquareWhite" => MapDecorationType.SquareWhite,
            "MarkerSign" => MapDecorationType.MarkerSign,
            "MarkerPink" => MapDecorationType.MarkerPink,
            "MarkerOrange" => MapDecorationType.MarkerOrange,
            "MarkerYellow" => MapDecorationType.MarkerYellow,
            "MarkerTeal" => MapDecorationType.MarkerTeal,
            "TriangleGreen" => MapDecorationType.TriangleGreen,
            "SmallSquareWhite" => MapDecorationType.SmallSquareWhite,
            "Mansion" => MapDecorationType.Mansion,
            "Monument" => MapDecorationType.Monument,
            "NoDraw" => MapDecorationType.NoDraw,
            "VillageDesert" => MapDecorationType.VillageDesert,
            "VillagePlains" => MapDecorationType.VillagePlains,
            "VillageSavanna" => MapDecorationType.VillageSavanna,
            "VillageSnowy" => MapDecorationType.VillageSnowy,
            "VillageTaiga" => MapDecorationType.VillageTaiga,
            "JungleTemple" => MapDecorationType.JungleTemple,
            "WitchHut" => MapDecorationType.WitchHut,
            "TrialChambers" => MapDecorationType.TrialChambers,
            "Count" => MapDecorationType.Count,
            _ => throw new ArgumentException($"Unknown MapDecorationType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out MapDecorationType result) {
        switch (value) {
            case "MarkerWhite":
                result = MapDecorationType.MarkerWhite;
                return true;
            case "MarkerGreen":
                result = MapDecorationType.MarkerGreen;
                return true;
            case "MarkerRed":
                result = MapDecorationType.MarkerRed;
                return true;
            case "MarkerBlue":
                result = MapDecorationType.MarkerBlue;
                return true;
            case "XWhite":
                result = MapDecorationType.XWhite;
                return true;
            case "TriangleRed":
                result = MapDecorationType.TriangleRed;
                return true;
            case "SquareWhite":
                result = MapDecorationType.SquareWhite;
                return true;
            case "MarkerSign":
                result = MapDecorationType.MarkerSign;
                return true;
            case "MarkerPink":
                result = MapDecorationType.MarkerPink;
                return true;
            case "MarkerOrange":
                result = MapDecorationType.MarkerOrange;
                return true;
            case "MarkerYellow":
                result = MapDecorationType.MarkerYellow;
                return true;
            case "MarkerTeal":
                result = MapDecorationType.MarkerTeal;
                return true;
            case "TriangleGreen":
                result = MapDecorationType.TriangleGreen;
                return true;
            case "SmallSquareWhite":
                result = MapDecorationType.SmallSquareWhite;
                return true;
            case "Mansion":
                result = MapDecorationType.Mansion;
                return true;
            case "Monument":
                result = MapDecorationType.Monument;
                return true;
            case "NoDraw":
                result = MapDecorationType.NoDraw;
                return true;
            case "VillageDesert":
                result = MapDecorationType.VillageDesert;
                return true;
            case "VillagePlains":
                result = MapDecorationType.VillagePlains;
                return true;
            case "VillageSavanna":
                result = MapDecorationType.VillageSavanna;
                return true;
            case "VillageSnowy":
                result = MapDecorationType.VillageSnowy;
                return true;
            case "VillageTaiga":
                result = MapDecorationType.VillageTaiga;
                return true;
            case "JungleTemple":
                result = MapDecorationType.JungleTemple;
                return true;
            case "WitchHut":
                result = MapDecorationType.WitchHut;
                return true;
            case "TrialChambers":
                result = MapDecorationType.TrialChambers;
                return true;
            case "Count":
                result = MapDecorationType.Count;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

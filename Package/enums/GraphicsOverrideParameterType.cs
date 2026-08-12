#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum GraphicsOverrideParameterType {
    SkyZenithColor = 0,
    SkyHorizonColor = 1,
    HorizonBlendMin = 2,
    HorizonBlendMax = 3,
    HorizonBlendStart = 4,
    HorizonBlendMieStart = 5,
    RayleighStrength = 6,
    SunMieStrength = 7,
    MoonMieStrength = 8,
    SunGlareShape = 9,
    Chlorophyll = 10,
    CDOM = 11,
    SuspendedSediment = 12,
    WavesDepth = 13,
    WavesFrequency = 14,
    WavesFrequencyScaling = 15,
    WavesSpeed = 16,
    WavesSpeedScaling = 17,
    WavesShape = 18,
    WavesOctaves = 19,
    WavesMix = 20,
    WavesPull = 21,
    WavesDirectionIncrement = 22,
    MidtonesContrast = 23,
    HighlightsContrast = 24,
    ShadowsContrast = 25,
    HighlightsGain = 26,
    HighlightsGamma = 27,
    HighlightsOffset = 28,
    HighlightsSaturation = 29,
    MidtonesGain = 30,
    MidtonesGamma = 31,
    MidtonesOffset = 32,
    MidtonesSaturation = 33,
    ShadowsGain = 34,
    ShadowsGamma = 35,
    ShadowsOffset = 36,
    ShadowsSaturation = 37,
    HighlightsMin = 38,
    ShadowsMax = 39,
    Temperature = 40,
    SunColor = 41,
    SunIlluminance = 42,
    MoonColor = 43,
    MoonIlluminance = 44,
    FlashColor = 45,
    FlashIlluminance = 46,
    AmbientColor = 47,
    AmbientIlluminance = 48,
    EmissiveDesaturation = 49,
    SkyIntensity = 50,
    OrbitalOffsetDegrees = 51,
}

public static class GraphicsOverrideParameterTypeExtensions {
    public static string ToProtoString(this GraphicsOverrideParameterType value) => value.ToProtocolString();

    public static string ToProtocolString(this GraphicsOverrideParameterType value) {
        return value switch {
            GraphicsOverrideParameterType.SkyZenithColor => "SkyZenithColor",
            GraphicsOverrideParameterType.SkyHorizonColor => "SkyHorizonColor",
            GraphicsOverrideParameterType.HorizonBlendMin => "HorizonBlendMin",
            GraphicsOverrideParameterType.HorizonBlendMax => "HorizonBlendMax",
            GraphicsOverrideParameterType.HorizonBlendStart => "HorizonBlendStart",
            GraphicsOverrideParameterType.HorizonBlendMieStart => "HorizonBlendMieStart",
            GraphicsOverrideParameterType.RayleighStrength => "RayleighStrength",
            GraphicsOverrideParameterType.SunMieStrength => "SunMieStrength",
            GraphicsOverrideParameterType.MoonMieStrength => "MoonMieStrength",
            GraphicsOverrideParameterType.SunGlareShape => "SunGlareShape",
            GraphicsOverrideParameterType.Chlorophyll => "Chlorophyll",
            GraphicsOverrideParameterType.CDOM => "CDOM",
            GraphicsOverrideParameterType.SuspendedSediment => "SuspendedSediment",
            GraphicsOverrideParameterType.WavesDepth => "WavesDepth",
            GraphicsOverrideParameterType.WavesFrequency => "WavesFrequency",
            GraphicsOverrideParameterType.WavesFrequencyScaling => "WavesFrequencyScaling",
            GraphicsOverrideParameterType.WavesSpeed => "WavesSpeed",
            GraphicsOverrideParameterType.WavesSpeedScaling => "WavesSpeedScaling",
            GraphicsOverrideParameterType.WavesShape => "WavesShape",
            GraphicsOverrideParameterType.WavesOctaves => "WavesOctaves",
            GraphicsOverrideParameterType.WavesMix => "WavesMix",
            GraphicsOverrideParameterType.WavesPull => "WavesPull",
            GraphicsOverrideParameterType.WavesDirectionIncrement => "WavesDirectionIncrement",
            GraphicsOverrideParameterType.MidtonesContrast => "MidtonesContrast",
            GraphicsOverrideParameterType.HighlightsContrast => "HighlightsContrast",
            GraphicsOverrideParameterType.ShadowsContrast => "ShadowsContrast",
            GraphicsOverrideParameterType.HighlightsGain => "HighlightsGain",
            GraphicsOverrideParameterType.HighlightsGamma => "HighlightsGamma",
            GraphicsOverrideParameterType.HighlightsOffset => "HighlightsOffset",
            GraphicsOverrideParameterType.HighlightsSaturation => "HighlightsSaturation",
            GraphicsOverrideParameterType.MidtonesGain => "MidtonesGain",
            GraphicsOverrideParameterType.MidtonesGamma => "MidtonesGamma",
            GraphicsOverrideParameterType.MidtonesOffset => "MidtonesOffset",
            GraphicsOverrideParameterType.MidtonesSaturation => "MidtonesSaturation",
            GraphicsOverrideParameterType.ShadowsGain => "ShadowsGain",
            GraphicsOverrideParameterType.ShadowsGamma => "ShadowsGamma",
            GraphicsOverrideParameterType.ShadowsOffset => "ShadowsOffset",
            GraphicsOverrideParameterType.ShadowsSaturation => "ShadowsSaturation",
            GraphicsOverrideParameterType.HighlightsMin => "HighlightsMin",
            GraphicsOverrideParameterType.ShadowsMax => "ShadowsMax",
            GraphicsOverrideParameterType.Temperature => "Temperature",
            GraphicsOverrideParameterType.SunColor => "SunColor",
            GraphicsOverrideParameterType.SunIlluminance => "SunIlluminance",
            GraphicsOverrideParameterType.MoonColor => "MoonColor",
            GraphicsOverrideParameterType.MoonIlluminance => "MoonIlluminance",
            GraphicsOverrideParameterType.FlashColor => "FlashColor",
            GraphicsOverrideParameterType.FlashIlluminance => "FlashIlluminance",
            GraphicsOverrideParameterType.AmbientColor => "AmbientColor",
            GraphicsOverrideParameterType.AmbientIlluminance => "AmbientIlluminance",
            GraphicsOverrideParameterType.EmissiveDesaturation => "EmissiveDesaturation",
            GraphicsOverrideParameterType.SkyIntensity => "SkyIntensity",
            GraphicsOverrideParameterType.OrbitalOffsetDegrees => "OrbitalOffsetDegrees",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown GraphicsOverrideParameterType value.")
        };
    }

    public static GraphicsOverrideParameterType FromProtocolString(string value) {
        return value switch {
            "SkyZenithColor" => GraphicsOverrideParameterType.SkyZenithColor,
            "SkyHorizonColor" => GraphicsOverrideParameterType.SkyHorizonColor,
            "HorizonBlendMin" => GraphicsOverrideParameterType.HorizonBlendMin,
            "HorizonBlendMax" => GraphicsOverrideParameterType.HorizonBlendMax,
            "HorizonBlendStart" => GraphicsOverrideParameterType.HorizonBlendStart,
            "HorizonBlendMieStart" => GraphicsOverrideParameterType.HorizonBlendMieStart,
            "RayleighStrength" => GraphicsOverrideParameterType.RayleighStrength,
            "SunMieStrength" => GraphicsOverrideParameterType.SunMieStrength,
            "MoonMieStrength" => GraphicsOverrideParameterType.MoonMieStrength,
            "SunGlareShape" => GraphicsOverrideParameterType.SunGlareShape,
            "Chlorophyll" => GraphicsOverrideParameterType.Chlorophyll,
            "CDOM" => GraphicsOverrideParameterType.CDOM,
            "SuspendedSediment" => GraphicsOverrideParameterType.SuspendedSediment,
            "WavesDepth" => GraphicsOverrideParameterType.WavesDepth,
            "WavesFrequency" => GraphicsOverrideParameterType.WavesFrequency,
            "WavesFrequencyScaling" => GraphicsOverrideParameterType.WavesFrequencyScaling,
            "WavesSpeed" => GraphicsOverrideParameterType.WavesSpeed,
            "WavesSpeedScaling" => GraphicsOverrideParameterType.WavesSpeedScaling,
            "WavesShape" => GraphicsOverrideParameterType.WavesShape,
            "WavesOctaves" => GraphicsOverrideParameterType.WavesOctaves,
            "WavesMix" => GraphicsOverrideParameterType.WavesMix,
            "WavesPull" => GraphicsOverrideParameterType.WavesPull,
            "WavesDirectionIncrement" => GraphicsOverrideParameterType.WavesDirectionIncrement,
            "MidtonesContrast" => GraphicsOverrideParameterType.MidtonesContrast,
            "HighlightsContrast" => GraphicsOverrideParameterType.HighlightsContrast,
            "ShadowsContrast" => GraphicsOverrideParameterType.ShadowsContrast,
            "HighlightsGain" => GraphicsOverrideParameterType.HighlightsGain,
            "HighlightsGamma" => GraphicsOverrideParameterType.HighlightsGamma,
            "HighlightsOffset" => GraphicsOverrideParameterType.HighlightsOffset,
            "HighlightsSaturation" => GraphicsOverrideParameterType.HighlightsSaturation,
            "MidtonesGain" => GraphicsOverrideParameterType.MidtonesGain,
            "MidtonesGamma" => GraphicsOverrideParameterType.MidtonesGamma,
            "MidtonesOffset" => GraphicsOverrideParameterType.MidtonesOffset,
            "MidtonesSaturation" => GraphicsOverrideParameterType.MidtonesSaturation,
            "ShadowsGain" => GraphicsOverrideParameterType.ShadowsGain,
            "ShadowsGamma" => GraphicsOverrideParameterType.ShadowsGamma,
            "ShadowsOffset" => GraphicsOverrideParameterType.ShadowsOffset,
            "ShadowsSaturation" => GraphicsOverrideParameterType.ShadowsSaturation,
            "HighlightsMin" => GraphicsOverrideParameterType.HighlightsMin,
            "ShadowsMax" => GraphicsOverrideParameterType.ShadowsMax,
            "Temperature" => GraphicsOverrideParameterType.Temperature,
            "SunColor" => GraphicsOverrideParameterType.SunColor,
            "SunIlluminance" => GraphicsOverrideParameterType.SunIlluminance,
            "MoonColor" => GraphicsOverrideParameterType.MoonColor,
            "MoonIlluminance" => GraphicsOverrideParameterType.MoonIlluminance,
            "FlashColor" => GraphicsOverrideParameterType.FlashColor,
            "FlashIlluminance" => GraphicsOverrideParameterType.FlashIlluminance,
            "AmbientColor" => GraphicsOverrideParameterType.AmbientColor,
            "AmbientIlluminance" => GraphicsOverrideParameterType.AmbientIlluminance,
            "EmissiveDesaturation" => GraphicsOverrideParameterType.EmissiveDesaturation,
            "SkyIntensity" => GraphicsOverrideParameterType.SkyIntensity,
            "OrbitalOffsetDegrees" => GraphicsOverrideParameterType.OrbitalOffsetDegrees,
            _ => throw new ArgumentException($"Unknown GraphicsOverrideParameterType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out GraphicsOverrideParameterType result) {
        switch (value) {
            case "SkyZenithColor":
                result = GraphicsOverrideParameterType.SkyZenithColor;
                return true;
            case "SkyHorizonColor":
                result = GraphicsOverrideParameterType.SkyHorizonColor;
                return true;
            case "HorizonBlendMin":
                result = GraphicsOverrideParameterType.HorizonBlendMin;
                return true;
            case "HorizonBlendMax":
                result = GraphicsOverrideParameterType.HorizonBlendMax;
                return true;
            case "HorizonBlendStart":
                result = GraphicsOverrideParameterType.HorizonBlendStart;
                return true;
            case "HorizonBlendMieStart":
                result = GraphicsOverrideParameterType.HorizonBlendMieStart;
                return true;
            case "RayleighStrength":
                result = GraphicsOverrideParameterType.RayleighStrength;
                return true;
            case "SunMieStrength":
                result = GraphicsOverrideParameterType.SunMieStrength;
                return true;
            case "MoonMieStrength":
                result = GraphicsOverrideParameterType.MoonMieStrength;
                return true;
            case "SunGlareShape":
                result = GraphicsOverrideParameterType.SunGlareShape;
                return true;
            case "Chlorophyll":
                result = GraphicsOverrideParameterType.Chlorophyll;
                return true;
            case "CDOM":
                result = GraphicsOverrideParameterType.CDOM;
                return true;
            case "SuspendedSediment":
                result = GraphicsOverrideParameterType.SuspendedSediment;
                return true;
            case "WavesDepth":
                result = GraphicsOverrideParameterType.WavesDepth;
                return true;
            case "WavesFrequency":
                result = GraphicsOverrideParameterType.WavesFrequency;
                return true;
            case "WavesFrequencyScaling":
                result = GraphicsOverrideParameterType.WavesFrequencyScaling;
                return true;
            case "WavesSpeed":
                result = GraphicsOverrideParameterType.WavesSpeed;
                return true;
            case "WavesSpeedScaling":
                result = GraphicsOverrideParameterType.WavesSpeedScaling;
                return true;
            case "WavesShape":
                result = GraphicsOverrideParameterType.WavesShape;
                return true;
            case "WavesOctaves":
                result = GraphicsOverrideParameterType.WavesOctaves;
                return true;
            case "WavesMix":
                result = GraphicsOverrideParameterType.WavesMix;
                return true;
            case "WavesPull":
                result = GraphicsOverrideParameterType.WavesPull;
                return true;
            case "WavesDirectionIncrement":
                result = GraphicsOverrideParameterType.WavesDirectionIncrement;
                return true;
            case "MidtonesContrast":
                result = GraphicsOverrideParameterType.MidtonesContrast;
                return true;
            case "HighlightsContrast":
                result = GraphicsOverrideParameterType.HighlightsContrast;
                return true;
            case "ShadowsContrast":
                result = GraphicsOverrideParameterType.ShadowsContrast;
                return true;
            case "HighlightsGain":
                result = GraphicsOverrideParameterType.HighlightsGain;
                return true;
            case "HighlightsGamma":
                result = GraphicsOverrideParameterType.HighlightsGamma;
                return true;
            case "HighlightsOffset":
                result = GraphicsOverrideParameterType.HighlightsOffset;
                return true;
            case "HighlightsSaturation":
                result = GraphicsOverrideParameterType.HighlightsSaturation;
                return true;
            case "MidtonesGain":
                result = GraphicsOverrideParameterType.MidtonesGain;
                return true;
            case "MidtonesGamma":
                result = GraphicsOverrideParameterType.MidtonesGamma;
                return true;
            case "MidtonesOffset":
                result = GraphicsOverrideParameterType.MidtonesOffset;
                return true;
            case "MidtonesSaturation":
                result = GraphicsOverrideParameterType.MidtonesSaturation;
                return true;
            case "ShadowsGain":
                result = GraphicsOverrideParameterType.ShadowsGain;
                return true;
            case "ShadowsGamma":
                result = GraphicsOverrideParameterType.ShadowsGamma;
                return true;
            case "ShadowsOffset":
                result = GraphicsOverrideParameterType.ShadowsOffset;
                return true;
            case "ShadowsSaturation":
                result = GraphicsOverrideParameterType.ShadowsSaturation;
                return true;
            case "HighlightsMin":
                result = GraphicsOverrideParameterType.HighlightsMin;
                return true;
            case "ShadowsMax":
                result = GraphicsOverrideParameterType.ShadowsMax;
                return true;
            case "Temperature":
                result = GraphicsOverrideParameterType.Temperature;
                return true;
            case "SunColor":
                result = GraphicsOverrideParameterType.SunColor;
                return true;
            case "SunIlluminance":
                result = GraphicsOverrideParameterType.SunIlluminance;
                return true;
            case "MoonColor":
                result = GraphicsOverrideParameterType.MoonColor;
                return true;
            case "MoonIlluminance":
                result = GraphicsOverrideParameterType.MoonIlluminance;
                return true;
            case "FlashColor":
                result = GraphicsOverrideParameterType.FlashColor;
                return true;
            case "FlashIlluminance":
                result = GraphicsOverrideParameterType.FlashIlluminance;
                return true;
            case "AmbientColor":
                result = GraphicsOverrideParameterType.AmbientColor;
                return true;
            case "AmbientIlluminance":
                result = GraphicsOverrideParameterType.AmbientIlluminance;
                return true;
            case "EmissiveDesaturation":
                result = GraphicsOverrideParameterType.EmissiveDesaturation;
                return true;
            case "SkyIntensity":
                result = GraphicsOverrideParameterType.SkyIntensity;
                return true;
            case "OrbitalOffsetDegrees":
                result = GraphicsOverrideParameterType.OrbitalOffsetDegrees;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

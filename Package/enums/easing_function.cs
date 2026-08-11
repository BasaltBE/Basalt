using System;

namespace BedrockProtocol.Enums;

public enum easing_function {
    linear = 0,
    spring = 1,
    in_quad = 2,
    out_quad = 3,
    in_out_quad = 4,
    in_cubic = 5,
    out_cubic = 6,
    in_out_cubic = 7,
    in_quart = 8,
    out_quart = 9,
    in_out_quart = 10,
    in_quint = 11,
    out_quint = 12,
    in_out_quint = 13,
    in_sine = 14,
    out_sine = 15,
    in_out_sine = 16,
    in_expo = 17,
    out_expo = 18,
    in_out_expo = 19,
    in_circ = 20,
    out_circ = 21,
    in_out_circ = 22,
    in_bounce = 23,
    out_bounce = 24,
    in_out_bounce = 25,
    in_back = 26,
    out_back = 27,
    in_out_back = 28,
    in_elastic = 29,
    out_elastic = 30,
    in_out_elastic = 31,
}

public static class easing_functionExtensions {
    public static string ToProtoString(this easing_function value) => value.ToProtocolString();

    public static string ToProtocolString(this easing_function value) {
        return value switch {
            easing_function.linear => "linear",
            easing_function.spring => "spring",
            easing_function.in_quad => "in_quad",
            easing_function.out_quad => "out_quad",
            easing_function.in_out_quad => "in_out_quad",
            easing_function.in_cubic => "in_cubic",
            easing_function.out_cubic => "out_cubic",
            easing_function.in_out_cubic => "in_out_cubic",
            easing_function.in_quart => "in_quart",
            easing_function.out_quart => "out_quart",
            easing_function.in_out_quart => "in_out_quart",
            easing_function.in_quint => "in_quint",
            easing_function.out_quint => "out_quint",
            easing_function.in_out_quint => "in_out_quint",
            easing_function.in_sine => "in_sine",
            easing_function.out_sine => "out_sine",
            easing_function.in_out_sine => "in_out_sine",
            easing_function.in_expo => "in_expo",
            easing_function.out_expo => "out_expo",
            easing_function.in_out_expo => "in_out_expo",
            easing_function.in_circ => "in_circ",
            easing_function.out_circ => "out_circ",
            easing_function.in_out_circ => "in_out_circ",
            easing_function.in_bounce => "in_bounce",
            easing_function.out_bounce => "out_bounce",
            easing_function.in_out_bounce => "in_out_bounce",
            easing_function.in_back => "in_back",
            easing_function.out_back => "out_back",
            easing_function.in_out_back => "in_out_back",
            easing_function.in_elastic => "in_elastic",
            easing_function.out_elastic => "out_elastic",
            easing_function.in_out_elastic => "in_out_elastic",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown easing_function value.")
        };
    }

    public static easing_function FromProtocolString(string value) {
        return value switch {
            "linear" => easing_function.linear,
            "spring" => easing_function.spring,
            "in_quad" => easing_function.in_quad,
            "out_quad" => easing_function.out_quad,
            "in_out_quad" => easing_function.in_out_quad,
            "in_cubic" => easing_function.in_cubic,
            "out_cubic" => easing_function.out_cubic,
            "in_out_cubic" => easing_function.in_out_cubic,
            "in_quart" => easing_function.in_quart,
            "out_quart" => easing_function.out_quart,
            "in_out_quart" => easing_function.in_out_quart,
            "in_quint" => easing_function.in_quint,
            "out_quint" => easing_function.out_quint,
            "in_out_quint" => easing_function.in_out_quint,
            "in_sine" => easing_function.in_sine,
            "out_sine" => easing_function.out_sine,
            "in_out_sine" => easing_function.in_out_sine,
            "in_expo" => easing_function.in_expo,
            "out_expo" => easing_function.out_expo,
            "in_out_expo" => easing_function.in_out_expo,
            "in_circ" => easing_function.in_circ,
            "out_circ" => easing_function.out_circ,
            "in_out_circ" => easing_function.in_out_circ,
            "in_bounce" => easing_function.in_bounce,
            "out_bounce" => easing_function.out_bounce,
            "in_out_bounce" => easing_function.in_out_bounce,
            "in_back" => easing_function.in_back,
            "out_back" => easing_function.out_back,
            "in_out_back" => easing_function.in_out_back,
            "in_elastic" => easing_function.in_elastic,
            "out_elastic" => easing_function.out_elastic,
            "in_out_elastic" => easing_function.in_out_elastic,
            _ => throw new ArgumentException($"Unknown easing_function protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out easing_function result) {
        switch (value) {
            case "linear":
                result = easing_function.linear;
                return true;
            case "spring":
                result = easing_function.spring;
                return true;
            case "in_quad":
                result = easing_function.in_quad;
                return true;
            case "out_quad":
                result = easing_function.out_quad;
                return true;
            case "in_out_quad":
                result = easing_function.in_out_quad;
                return true;
            case "in_cubic":
                result = easing_function.in_cubic;
                return true;
            case "out_cubic":
                result = easing_function.out_cubic;
                return true;
            case "in_out_cubic":
                result = easing_function.in_out_cubic;
                return true;
            case "in_quart":
                result = easing_function.in_quart;
                return true;
            case "out_quart":
                result = easing_function.out_quart;
                return true;
            case "in_out_quart":
                result = easing_function.in_out_quart;
                return true;
            case "in_quint":
                result = easing_function.in_quint;
                return true;
            case "out_quint":
                result = easing_function.out_quint;
                return true;
            case "in_out_quint":
                result = easing_function.in_out_quint;
                return true;
            case "in_sine":
                result = easing_function.in_sine;
                return true;
            case "out_sine":
                result = easing_function.out_sine;
                return true;
            case "in_out_sine":
                result = easing_function.in_out_sine;
                return true;
            case "in_expo":
                result = easing_function.in_expo;
                return true;
            case "out_expo":
                result = easing_function.out_expo;
                return true;
            case "in_out_expo":
                result = easing_function.in_out_expo;
                return true;
            case "in_circ":
                result = easing_function.in_circ;
                return true;
            case "out_circ":
                result = easing_function.out_circ;
                return true;
            case "in_out_circ":
                result = easing_function.in_out_circ;
                return true;
            case "in_bounce":
                result = easing_function.in_bounce;
                return true;
            case "out_bounce":
                result = easing_function.out_bounce;
                return true;
            case "in_out_bounce":
                result = easing_function.in_out_bounce;
                return true;
            case "in_back":
                result = easing_function.in_back;
                return true;
            case "out_back":
                result = easing_function.out_back;
                return true;
            case "in_out_back":
                result = easing_function.in_out_back;
                return true;
            case "in_elastic":
                result = easing_function.in_elastic;
                return true;
            case "out_elastic":
                result = easing_function.out_elastic;
                return true;
            case "in_out_elastic":
                result = easing_function.in_out_elastic;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

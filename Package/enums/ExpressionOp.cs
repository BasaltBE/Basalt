#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ExpressionOp {
    Unknown = -1,
    LeftBrace = 0,
    RightBrace = 1,
    LeftBracket = 2,
    RightBracket = 3,
    LeftParenthesis = 4,
    RightParenthesis = 5,
    Negate = 6,
    LogicalNot = 7,
    Abs = 8,
    Add = 9,
    Acos = 10,
    Asin = 11,
    Atan = 12,
    Atan2 = 13,
    Ceil = 14,
    Clamp = 15,
    CopySign = 16,
    Cos = 17,
    DieRoll = 18,
    DieRollInt = 19,
    Div = 20,
    Exp = 21,
    Floor = 22,
    HermiteBlend = 23,
    Lerp = 24,
    LerpRotate = 25,
    Ln = 26,
    Max = 27,
    Min = 28,
    MinAngle = 29,
    Mod = 30,
    Mul = 31,
    Pow = 32,
    Random = 33,
    RandomInt = 34,
    Round = 35,
    Sin = 36,
    Sign = 37,
    Sqrt = 38,
    Trunc = 39,
    QueryFunction = 40,
    ArrayVariable = 41,
    ContextVariable = 42,
    EntityVariable = 43,
    TempVariable = 44,
    MemberAccessor = 45,
    HashedStringHash = 46,
    GeometryVariable = 47,
    MaterialVariable = 48,
    TextureVariable = 49,
    LessThan = 50,
    LessEqual = 51,
    GreaterEqual = 52,
    GreaterThan = 53,
    LogicalEqual = 54,
    LogicalNotEqual = 55,
    LogicalOr = 56,
    LogicalAnd = 57,
    NullCoalescing = 58,
    Conditional = 59,
    ConditionalElse = 60,
    Float = 61,
    Pi = 62,
    Array = 63,
    Geometry = 64,
    Material = 65,
    Texture = 66,
    Loop = 67,
    ForEach = 68,
    Break = 69,
    Continue = 70,
    Assignment = 71,
    Pointer = 72,
    Semicolon = 73,
    Return = 74,
    Comma = 75,
    This = 76,
    Internal_NonEvaluatedArray = 77,
    InverseLerp = 78,
    EaseInQuad = 79,
    EaseOutQuad = 80,
    EaseInOutQuad = 81,
    EaseInCubic = 82,
    EaseOutCubic = 83,
    EaseInOutCubic = 84,
    EaseInQuart = 85,
    EaseOutQuart = 86,
    EaseInOutQuart = 87,
    EaseInQuint = 88,
    EaseOutQuint = 89,
    EaseInOutQuint = 90,
    EaseInSine = 91,
    EaseOutSine = 92,
    EaseInOutSine = 93,
    EaseInExpo = 94,
    EaseOutExpo = 95,
    EaseInOutExpo = 96,
    EaseInCirc = 97,
    EaseOutCirc = 98,
    EaseInOutCirc = 99,
    EaseInBounce = 100,
    EaseOutBounce = 101,
    EaseInOutBounce = 102,
    EaseInBack = 103,
    EaseOutBack = 104,
    EaseInOutBack = 105,
    EaseInElastic = 106,
    EaseOutElastic = 107,
    EaseInOutElastic = 108,
}

public static class ExpressionOpExtensions {
    public static string ToProtoString(this ExpressionOp value) => value.ToProtocolString();

    public static string ToProtocolString(this ExpressionOp value) {
        return value switch {
            ExpressionOp.Unknown => "Unknown",
            ExpressionOp.LeftBrace => "LeftBrace",
            ExpressionOp.RightBrace => "RightBrace",
            ExpressionOp.LeftBracket => "LeftBracket",
            ExpressionOp.RightBracket => "RightBracket",
            ExpressionOp.LeftParenthesis => "LeftParenthesis",
            ExpressionOp.RightParenthesis => "RightParenthesis",
            ExpressionOp.Negate => "Negate",
            ExpressionOp.LogicalNot => "LogicalNot",
            ExpressionOp.Abs => "Abs",
            ExpressionOp.Add => "Add",
            ExpressionOp.Acos => "Acos",
            ExpressionOp.Asin => "Asin",
            ExpressionOp.Atan => "Atan",
            ExpressionOp.Atan2 => "Atan2",
            ExpressionOp.Ceil => "Ceil",
            ExpressionOp.Clamp => "Clamp",
            ExpressionOp.CopySign => "CopySign",
            ExpressionOp.Cos => "Cos",
            ExpressionOp.DieRoll => "DieRoll",
            ExpressionOp.DieRollInt => "DieRollInt",
            ExpressionOp.Div => "Div",
            ExpressionOp.Exp => "Exp",
            ExpressionOp.Floor => "Floor",
            ExpressionOp.HermiteBlend => "HermiteBlend",
            ExpressionOp.Lerp => "Lerp",
            ExpressionOp.LerpRotate => "LerpRotate",
            ExpressionOp.Ln => "Ln",
            ExpressionOp.Max => "Max",
            ExpressionOp.Min => "Min",
            ExpressionOp.MinAngle => "MinAngle",
            ExpressionOp.Mod => "Mod",
            ExpressionOp.Mul => "Mul",
            ExpressionOp.Pow => "Pow",
            ExpressionOp.Random => "Random",
            ExpressionOp.RandomInt => "RandomInt",
            ExpressionOp.Round => "Round",
            ExpressionOp.Sin => "Sin",
            ExpressionOp.Sign => "Sign",
            ExpressionOp.Sqrt => "Sqrt",
            ExpressionOp.Trunc => "Trunc",
            ExpressionOp.QueryFunction => "QueryFunction",
            ExpressionOp.ArrayVariable => "ArrayVariable",
            ExpressionOp.ContextVariable => "ContextVariable",
            ExpressionOp.EntityVariable => "EntityVariable",
            ExpressionOp.TempVariable => "TempVariable",
            ExpressionOp.MemberAccessor => "MemberAccessor",
            ExpressionOp.HashedStringHash => "HashedStringHash",
            ExpressionOp.GeometryVariable => "GeometryVariable",
            ExpressionOp.MaterialVariable => "MaterialVariable",
            ExpressionOp.TextureVariable => "TextureVariable",
            ExpressionOp.LessThan => "LessThan",
            ExpressionOp.LessEqual => "LessEqual",
            ExpressionOp.GreaterEqual => "GreaterEqual",
            ExpressionOp.GreaterThan => "GreaterThan",
            ExpressionOp.LogicalEqual => "LogicalEqual",
            ExpressionOp.LogicalNotEqual => "LogicalNotEqual",
            ExpressionOp.LogicalOr => "LogicalOr",
            ExpressionOp.LogicalAnd => "LogicalAnd",
            ExpressionOp.NullCoalescing => "NullCoalescing",
            ExpressionOp.Conditional => "Conditional",
            ExpressionOp.ConditionalElse => "ConditionalElse",
            ExpressionOp.Float => "Float",
            ExpressionOp.Pi => "Pi",
            ExpressionOp.Array => "Array",
            ExpressionOp.Geometry => "Geometry",
            ExpressionOp.Material => "Material",
            ExpressionOp.Texture => "Texture",
            ExpressionOp.Loop => "Loop",
            ExpressionOp.ForEach => "ForEach",
            ExpressionOp.Break => "Break",
            ExpressionOp.Continue => "Continue",
            ExpressionOp.Assignment => "Assignment",
            ExpressionOp.Pointer => "Pointer",
            ExpressionOp.Semicolon => "Semicolon",
            ExpressionOp.Return => "Return",
            ExpressionOp.Comma => "Comma",
            ExpressionOp.This => "This",
            ExpressionOp.Internal_NonEvaluatedArray => "Internal_NonEvaluatedArray",
            ExpressionOp.InverseLerp => "InverseLerp",
            ExpressionOp.EaseInQuad => "EaseInQuad",
            ExpressionOp.EaseOutQuad => "EaseOutQuad",
            ExpressionOp.EaseInOutQuad => "EaseInOutQuad",
            ExpressionOp.EaseInCubic => "EaseInCubic",
            ExpressionOp.EaseOutCubic => "EaseOutCubic",
            ExpressionOp.EaseInOutCubic => "EaseInOutCubic",
            ExpressionOp.EaseInQuart => "EaseInQuart",
            ExpressionOp.EaseOutQuart => "EaseOutQuart",
            ExpressionOp.EaseInOutQuart => "EaseInOutQuart",
            ExpressionOp.EaseInQuint => "EaseInQuint",
            ExpressionOp.EaseOutQuint => "EaseOutQuint",
            ExpressionOp.EaseInOutQuint => "EaseInOutQuint",
            ExpressionOp.EaseInSine => "EaseInSine",
            ExpressionOp.EaseOutSine => "EaseOutSine",
            ExpressionOp.EaseInOutSine => "EaseInOutSine",
            ExpressionOp.EaseInExpo => "EaseInExpo",
            ExpressionOp.EaseOutExpo => "EaseOutExpo",
            ExpressionOp.EaseInOutExpo => "EaseInOutExpo",
            ExpressionOp.EaseInCirc => "EaseInCirc",
            ExpressionOp.EaseOutCirc => "EaseOutCirc",
            ExpressionOp.EaseInOutCirc => "EaseInOutCirc",
            ExpressionOp.EaseInBounce => "EaseInBounce",
            ExpressionOp.EaseOutBounce => "EaseOutBounce",
            ExpressionOp.EaseInOutBounce => "EaseInOutBounce",
            ExpressionOp.EaseInBack => "EaseInBack",
            ExpressionOp.EaseOutBack => "EaseOutBack",
            ExpressionOp.EaseInOutBack => "EaseInOutBack",
            ExpressionOp.EaseInElastic => "EaseInElastic",
            ExpressionOp.EaseOutElastic => "EaseOutElastic",
            ExpressionOp.EaseInOutElastic => "EaseInOutElastic",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ExpressionOp value.")
        };
    }

    public static ExpressionOp FromProtocolString(string value) {
        return value switch {
            "Unknown" => ExpressionOp.Unknown,
            "LeftBrace" => ExpressionOp.LeftBrace,
            "RightBrace" => ExpressionOp.RightBrace,
            "LeftBracket" => ExpressionOp.LeftBracket,
            "RightBracket" => ExpressionOp.RightBracket,
            "LeftParenthesis" => ExpressionOp.LeftParenthesis,
            "RightParenthesis" => ExpressionOp.RightParenthesis,
            "Negate" => ExpressionOp.Negate,
            "LogicalNot" => ExpressionOp.LogicalNot,
            "Abs" => ExpressionOp.Abs,
            "Add" => ExpressionOp.Add,
            "Acos" => ExpressionOp.Acos,
            "Asin" => ExpressionOp.Asin,
            "Atan" => ExpressionOp.Atan,
            "Atan2" => ExpressionOp.Atan2,
            "Ceil" => ExpressionOp.Ceil,
            "Clamp" => ExpressionOp.Clamp,
            "CopySign" => ExpressionOp.CopySign,
            "Cos" => ExpressionOp.Cos,
            "DieRoll" => ExpressionOp.DieRoll,
            "DieRollInt" => ExpressionOp.DieRollInt,
            "Div" => ExpressionOp.Div,
            "Exp" => ExpressionOp.Exp,
            "Floor" => ExpressionOp.Floor,
            "HermiteBlend" => ExpressionOp.HermiteBlend,
            "Lerp" => ExpressionOp.Lerp,
            "LerpRotate" => ExpressionOp.LerpRotate,
            "Ln" => ExpressionOp.Ln,
            "Max" => ExpressionOp.Max,
            "Min" => ExpressionOp.Min,
            "MinAngle" => ExpressionOp.MinAngle,
            "Mod" => ExpressionOp.Mod,
            "Mul" => ExpressionOp.Mul,
            "Pow" => ExpressionOp.Pow,
            "Random" => ExpressionOp.Random,
            "RandomInt" => ExpressionOp.RandomInt,
            "Round" => ExpressionOp.Round,
            "Sin" => ExpressionOp.Sin,
            "Sign" => ExpressionOp.Sign,
            "Sqrt" => ExpressionOp.Sqrt,
            "Trunc" => ExpressionOp.Trunc,
            "QueryFunction" => ExpressionOp.QueryFunction,
            "ArrayVariable" => ExpressionOp.ArrayVariable,
            "ContextVariable" => ExpressionOp.ContextVariable,
            "EntityVariable" => ExpressionOp.EntityVariable,
            "TempVariable" => ExpressionOp.TempVariable,
            "MemberAccessor" => ExpressionOp.MemberAccessor,
            "HashedStringHash" => ExpressionOp.HashedStringHash,
            "GeometryVariable" => ExpressionOp.GeometryVariable,
            "MaterialVariable" => ExpressionOp.MaterialVariable,
            "TextureVariable" => ExpressionOp.TextureVariable,
            "LessThan" => ExpressionOp.LessThan,
            "LessEqual" => ExpressionOp.LessEqual,
            "GreaterEqual" => ExpressionOp.GreaterEqual,
            "GreaterThan" => ExpressionOp.GreaterThan,
            "LogicalEqual" => ExpressionOp.LogicalEqual,
            "LogicalNotEqual" => ExpressionOp.LogicalNotEqual,
            "LogicalOr" => ExpressionOp.LogicalOr,
            "LogicalAnd" => ExpressionOp.LogicalAnd,
            "NullCoalescing" => ExpressionOp.NullCoalescing,
            "Conditional" => ExpressionOp.Conditional,
            "ConditionalElse" => ExpressionOp.ConditionalElse,
            "Float" => ExpressionOp.Float,
            "Pi" => ExpressionOp.Pi,
            "Array" => ExpressionOp.Array,
            "Geometry" => ExpressionOp.Geometry,
            "Material" => ExpressionOp.Material,
            "Texture" => ExpressionOp.Texture,
            "Loop" => ExpressionOp.Loop,
            "ForEach" => ExpressionOp.ForEach,
            "Break" => ExpressionOp.Break,
            "Continue" => ExpressionOp.Continue,
            "Assignment" => ExpressionOp.Assignment,
            "Pointer" => ExpressionOp.Pointer,
            "Semicolon" => ExpressionOp.Semicolon,
            "Return" => ExpressionOp.Return,
            "Comma" => ExpressionOp.Comma,
            "This" => ExpressionOp.This,
            "Internal_NonEvaluatedArray" => ExpressionOp.Internal_NonEvaluatedArray,
            "InverseLerp" => ExpressionOp.InverseLerp,
            "EaseInQuad" => ExpressionOp.EaseInQuad,
            "EaseOutQuad" => ExpressionOp.EaseOutQuad,
            "EaseInOutQuad" => ExpressionOp.EaseInOutQuad,
            "EaseInCubic" => ExpressionOp.EaseInCubic,
            "EaseOutCubic" => ExpressionOp.EaseOutCubic,
            "EaseInOutCubic" => ExpressionOp.EaseInOutCubic,
            "EaseInQuart" => ExpressionOp.EaseInQuart,
            "EaseOutQuart" => ExpressionOp.EaseOutQuart,
            "EaseInOutQuart" => ExpressionOp.EaseInOutQuart,
            "EaseInQuint" => ExpressionOp.EaseInQuint,
            "EaseOutQuint" => ExpressionOp.EaseOutQuint,
            "EaseInOutQuint" => ExpressionOp.EaseInOutQuint,
            "EaseInSine" => ExpressionOp.EaseInSine,
            "EaseOutSine" => ExpressionOp.EaseOutSine,
            "EaseInOutSine" => ExpressionOp.EaseInOutSine,
            "EaseInExpo" => ExpressionOp.EaseInExpo,
            "EaseOutExpo" => ExpressionOp.EaseOutExpo,
            "EaseInOutExpo" => ExpressionOp.EaseInOutExpo,
            "EaseInCirc" => ExpressionOp.EaseInCirc,
            "EaseOutCirc" => ExpressionOp.EaseOutCirc,
            "EaseInOutCirc" => ExpressionOp.EaseInOutCirc,
            "EaseInBounce" => ExpressionOp.EaseInBounce,
            "EaseOutBounce" => ExpressionOp.EaseOutBounce,
            "EaseInOutBounce" => ExpressionOp.EaseInOutBounce,
            "EaseInBack" => ExpressionOp.EaseInBack,
            "EaseOutBack" => ExpressionOp.EaseOutBack,
            "EaseInOutBack" => ExpressionOp.EaseInOutBack,
            "EaseInElastic" => ExpressionOp.EaseInElastic,
            "EaseOutElastic" => ExpressionOp.EaseOutElastic,
            "EaseInOutElastic" => ExpressionOp.EaseInOutElastic,
            _ => throw new ArgumentException($"Unknown ExpressionOp protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ExpressionOp result) {
        switch (value) {
            case "Unknown":
                result = ExpressionOp.Unknown;
                return true;
            case "LeftBrace":
                result = ExpressionOp.LeftBrace;
                return true;
            case "RightBrace":
                result = ExpressionOp.RightBrace;
                return true;
            case "LeftBracket":
                result = ExpressionOp.LeftBracket;
                return true;
            case "RightBracket":
                result = ExpressionOp.RightBracket;
                return true;
            case "LeftParenthesis":
                result = ExpressionOp.LeftParenthesis;
                return true;
            case "RightParenthesis":
                result = ExpressionOp.RightParenthesis;
                return true;
            case "Negate":
                result = ExpressionOp.Negate;
                return true;
            case "LogicalNot":
                result = ExpressionOp.LogicalNot;
                return true;
            case "Abs":
                result = ExpressionOp.Abs;
                return true;
            case "Add":
                result = ExpressionOp.Add;
                return true;
            case "Acos":
                result = ExpressionOp.Acos;
                return true;
            case "Asin":
                result = ExpressionOp.Asin;
                return true;
            case "Atan":
                result = ExpressionOp.Atan;
                return true;
            case "Atan2":
                result = ExpressionOp.Atan2;
                return true;
            case "Ceil":
                result = ExpressionOp.Ceil;
                return true;
            case "Clamp":
                result = ExpressionOp.Clamp;
                return true;
            case "CopySign":
                result = ExpressionOp.CopySign;
                return true;
            case "Cos":
                result = ExpressionOp.Cos;
                return true;
            case "DieRoll":
                result = ExpressionOp.DieRoll;
                return true;
            case "DieRollInt":
                result = ExpressionOp.DieRollInt;
                return true;
            case "Div":
                result = ExpressionOp.Div;
                return true;
            case "Exp":
                result = ExpressionOp.Exp;
                return true;
            case "Floor":
                result = ExpressionOp.Floor;
                return true;
            case "HermiteBlend":
                result = ExpressionOp.HermiteBlend;
                return true;
            case "Lerp":
                result = ExpressionOp.Lerp;
                return true;
            case "LerpRotate":
                result = ExpressionOp.LerpRotate;
                return true;
            case "Ln":
                result = ExpressionOp.Ln;
                return true;
            case "Max":
                result = ExpressionOp.Max;
                return true;
            case "Min":
                result = ExpressionOp.Min;
                return true;
            case "MinAngle":
                result = ExpressionOp.MinAngle;
                return true;
            case "Mod":
                result = ExpressionOp.Mod;
                return true;
            case "Mul":
                result = ExpressionOp.Mul;
                return true;
            case "Pow":
                result = ExpressionOp.Pow;
                return true;
            case "Random":
                result = ExpressionOp.Random;
                return true;
            case "RandomInt":
                result = ExpressionOp.RandomInt;
                return true;
            case "Round":
                result = ExpressionOp.Round;
                return true;
            case "Sin":
                result = ExpressionOp.Sin;
                return true;
            case "Sign":
                result = ExpressionOp.Sign;
                return true;
            case "Sqrt":
                result = ExpressionOp.Sqrt;
                return true;
            case "Trunc":
                result = ExpressionOp.Trunc;
                return true;
            case "QueryFunction":
                result = ExpressionOp.QueryFunction;
                return true;
            case "ArrayVariable":
                result = ExpressionOp.ArrayVariable;
                return true;
            case "ContextVariable":
                result = ExpressionOp.ContextVariable;
                return true;
            case "EntityVariable":
                result = ExpressionOp.EntityVariable;
                return true;
            case "TempVariable":
                result = ExpressionOp.TempVariable;
                return true;
            case "MemberAccessor":
                result = ExpressionOp.MemberAccessor;
                return true;
            case "HashedStringHash":
                result = ExpressionOp.HashedStringHash;
                return true;
            case "GeometryVariable":
                result = ExpressionOp.GeometryVariable;
                return true;
            case "MaterialVariable":
                result = ExpressionOp.MaterialVariable;
                return true;
            case "TextureVariable":
                result = ExpressionOp.TextureVariable;
                return true;
            case "LessThan":
                result = ExpressionOp.LessThan;
                return true;
            case "LessEqual":
                result = ExpressionOp.LessEqual;
                return true;
            case "GreaterEqual":
                result = ExpressionOp.GreaterEqual;
                return true;
            case "GreaterThan":
                result = ExpressionOp.GreaterThan;
                return true;
            case "LogicalEqual":
                result = ExpressionOp.LogicalEqual;
                return true;
            case "LogicalNotEqual":
                result = ExpressionOp.LogicalNotEqual;
                return true;
            case "LogicalOr":
                result = ExpressionOp.LogicalOr;
                return true;
            case "LogicalAnd":
                result = ExpressionOp.LogicalAnd;
                return true;
            case "NullCoalescing":
                result = ExpressionOp.NullCoalescing;
                return true;
            case "Conditional":
                result = ExpressionOp.Conditional;
                return true;
            case "ConditionalElse":
                result = ExpressionOp.ConditionalElse;
                return true;
            case "Float":
                result = ExpressionOp.Float;
                return true;
            case "Pi":
                result = ExpressionOp.Pi;
                return true;
            case "Array":
                result = ExpressionOp.Array;
                return true;
            case "Geometry":
                result = ExpressionOp.Geometry;
                return true;
            case "Material":
                result = ExpressionOp.Material;
                return true;
            case "Texture":
                result = ExpressionOp.Texture;
                return true;
            case "Loop":
                result = ExpressionOp.Loop;
                return true;
            case "ForEach":
                result = ExpressionOp.ForEach;
                return true;
            case "Break":
                result = ExpressionOp.Break;
                return true;
            case "Continue":
                result = ExpressionOp.Continue;
                return true;
            case "Assignment":
                result = ExpressionOp.Assignment;
                return true;
            case "Pointer":
                result = ExpressionOp.Pointer;
                return true;
            case "Semicolon":
                result = ExpressionOp.Semicolon;
                return true;
            case "Return":
                result = ExpressionOp.Return;
                return true;
            case "Comma":
                result = ExpressionOp.Comma;
                return true;
            case "This":
                result = ExpressionOp.This;
                return true;
            case "Internal_NonEvaluatedArray":
                result = ExpressionOp.Internal_NonEvaluatedArray;
                return true;
            case "InverseLerp":
                result = ExpressionOp.InverseLerp;
                return true;
            case "EaseInQuad":
                result = ExpressionOp.EaseInQuad;
                return true;
            case "EaseOutQuad":
                result = ExpressionOp.EaseOutQuad;
                return true;
            case "EaseInOutQuad":
                result = ExpressionOp.EaseInOutQuad;
                return true;
            case "EaseInCubic":
                result = ExpressionOp.EaseInCubic;
                return true;
            case "EaseOutCubic":
                result = ExpressionOp.EaseOutCubic;
                return true;
            case "EaseInOutCubic":
                result = ExpressionOp.EaseInOutCubic;
                return true;
            case "EaseInQuart":
                result = ExpressionOp.EaseInQuart;
                return true;
            case "EaseOutQuart":
                result = ExpressionOp.EaseOutQuart;
                return true;
            case "EaseInOutQuart":
                result = ExpressionOp.EaseInOutQuart;
                return true;
            case "EaseInQuint":
                result = ExpressionOp.EaseInQuint;
                return true;
            case "EaseOutQuint":
                result = ExpressionOp.EaseOutQuint;
                return true;
            case "EaseInOutQuint":
                result = ExpressionOp.EaseInOutQuint;
                return true;
            case "EaseInSine":
                result = ExpressionOp.EaseInSine;
                return true;
            case "EaseOutSine":
                result = ExpressionOp.EaseOutSine;
                return true;
            case "EaseInOutSine":
                result = ExpressionOp.EaseInOutSine;
                return true;
            case "EaseInExpo":
                result = ExpressionOp.EaseInExpo;
                return true;
            case "EaseOutExpo":
                result = ExpressionOp.EaseOutExpo;
                return true;
            case "EaseInOutExpo":
                result = ExpressionOp.EaseInOutExpo;
                return true;
            case "EaseInCirc":
                result = ExpressionOp.EaseInCirc;
                return true;
            case "EaseOutCirc":
                result = ExpressionOp.EaseOutCirc;
                return true;
            case "EaseInOutCirc":
                result = ExpressionOp.EaseInOutCirc;
                return true;
            case "EaseInBounce":
                result = ExpressionOp.EaseInBounce;
                return true;
            case "EaseOutBounce":
                result = ExpressionOp.EaseOutBounce;
                return true;
            case "EaseInOutBounce":
                result = ExpressionOp.EaseInOutBounce;
                return true;
            case "EaseInBack":
                result = ExpressionOp.EaseInBack;
                return true;
            case "EaseOutBack":
                result = ExpressionOp.EaseOutBack;
                return true;
            case "EaseInOutBack":
                result = ExpressionOp.EaseInOutBack;
                return true;
            case "EaseInElastic":
                result = ExpressionOp.EaseInElastic;
                return true;
            case "EaseOutElastic":
                result = ExpressionOp.EaseOutElastic;
                return true;
            case "EaseInOutElastic":
                result = ExpressionOp.EaseInOutElastic;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

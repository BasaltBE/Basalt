using System;

namespace BedrockProtocol.Enums;

public enum MolangVersion {
    Invalid = -1,
    BeforeVersioning = 0,
    Initial = 1,
    FixedItemRemainingUseDurationQuery = 2,
    ExpressionErrorMessages = 3,
    UnexpectedOperatorErrors = 4,
    ConditionalOperatorAssociativity = 5,
    ComparisonAndLogicalOperatorPrecedence = 6,
    DivideByNegativeValue = 7,
    FixedCapeFlapAmountQuery = 8,
    QueryBlockPropertyRenamedToState = 9,
    DeprecateOldBlockQueryNames = 10,
    DeprecatedSnifferAndCamelQueries = 11,
    LeafSupportingInFirstSolidBlockBelow = 12,
    NumValidVersions = 14,
    Latest = 13,
    HardcodedMolang = 13,
}

public static class MolangVersionExtensions {
    public static string ToProtoString(this MolangVersion value) => value.ToProtocolString();

    public static string ToProtocolString(this MolangVersion value) {
        return value switch {
            MolangVersion.Invalid => "Invalid",
            MolangVersion.BeforeVersioning => "BeforeVersioning",
            MolangVersion.Initial => "Initial",
            MolangVersion.FixedItemRemainingUseDurationQuery => "FixedItemRemainingUseDurationQuery",
            MolangVersion.ExpressionErrorMessages => "ExpressionErrorMessages",
            MolangVersion.UnexpectedOperatorErrors => "UnexpectedOperatorErrors",
            MolangVersion.ConditionalOperatorAssociativity => "ConditionalOperatorAssociativity",
            MolangVersion.ComparisonAndLogicalOperatorPrecedence => "ComparisonAndLogicalOperatorPrecedence",
            MolangVersion.DivideByNegativeValue => "DivideByNegativeValue",
            MolangVersion.FixedCapeFlapAmountQuery => "FixedCapeFlapAmountQuery",
            MolangVersion.QueryBlockPropertyRenamedToState => "QueryBlockPropertyRenamedToState",
            MolangVersion.DeprecateOldBlockQueryNames => "DeprecateOldBlockQueryNames",
            MolangVersion.DeprecatedSnifferAndCamelQueries => "DeprecatedSnifferAndCamelQueries",
            MolangVersion.LeafSupportingInFirstSolidBlockBelow => "LeafSupportingInFirstSolidBlockBelow",
            MolangVersion.NumValidVersions => "NumValidVersions",
            MolangVersion.Latest => "Latest",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown MolangVersion value.")
        };
    }

    public static MolangVersion FromProtocolString(string value) {
        return value switch {
            "Invalid" => MolangVersion.Invalid,
            "BeforeVersioning" => MolangVersion.BeforeVersioning,
            "Initial" => MolangVersion.Initial,
            "FixedItemRemainingUseDurationQuery" => MolangVersion.FixedItemRemainingUseDurationQuery,
            "ExpressionErrorMessages" => MolangVersion.ExpressionErrorMessages,
            "UnexpectedOperatorErrors" => MolangVersion.UnexpectedOperatorErrors,
            "ConditionalOperatorAssociativity" => MolangVersion.ConditionalOperatorAssociativity,
            "ComparisonAndLogicalOperatorPrecedence" => MolangVersion.ComparisonAndLogicalOperatorPrecedence,
            "DivideByNegativeValue" => MolangVersion.DivideByNegativeValue,
            "FixedCapeFlapAmountQuery" => MolangVersion.FixedCapeFlapAmountQuery,
            "QueryBlockPropertyRenamedToState" => MolangVersion.QueryBlockPropertyRenamedToState,
            "DeprecateOldBlockQueryNames" => MolangVersion.DeprecateOldBlockQueryNames,
            "DeprecatedSnifferAndCamelQueries" => MolangVersion.DeprecatedSnifferAndCamelQueries,
            "LeafSupportingInFirstSolidBlockBelow" => MolangVersion.LeafSupportingInFirstSolidBlockBelow,
            "NumValidVersions" => MolangVersion.NumValidVersions,
            "Latest" => MolangVersion.Latest,
            "HardcodedMolang" => MolangVersion.HardcodedMolang,
            _ => throw new ArgumentException($"Unknown MolangVersion protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out MolangVersion result) {
        switch (value) {
            case "Invalid":
                result = MolangVersion.Invalid;
                return true;
            case "BeforeVersioning":
                result = MolangVersion.BeforeVersioning;
                return true;
            case "Initial":
                result = MolangVersion.Initial;
                return true;
            case "FixedItemRemainingUseDurationQuery":
                result = MolangVersion.FixedItemRemainingUseDurationQuery;
                return true;
            case "ExpressionErrorMessages":
                result = MolangVersion.ExpressionErrorMessages;
                return true;
            case "UnexpectedOperatorErrors":
                result = MolangVersion.UnexpectedOperatorErrors;
                return true;
            case "ConditionalOperatorAssociativity":
                result = MolangVersion.ConditionalOperatorAssociativity;
                return true;
            case "ComparisonAndLogicalOperatorPrecedence":
                result = MolangVersion.ComparisonAndLogicalOperatorPrecedence;
                return true;
            case "DivideByNegativeValue":
                result = MolangVersion.DivideByNegativeValue;
                return true;
            case "FixedCapeFlapAmountQuery":
                result = MolangVersion.FixedCapeFlapAmountQuery;
                return true;
            case "QueryBlockPropertyRenamedToState":
                result = MolangVersion.QueryBlockPropertyRenamedToState;
                return true;
            case "DeprecateOldBlockQueryNames":
                result = MolangVersion.DeprecateOldBlockQueryNames;
                return true;
            case "DeprecatedSnifferAndCamelQueries":
                result = MolangVersion.DeprecatedSnifferAndCamelQueries;
                return true;
            case "LeafSupportingInFirstSolidBlockBelow":
                result = MolangVersion.LeafSupportingInFirstSolidBlockBelow;
                return true;
            case "NumValidVersions":
                result = MolangVersion.NumValidVersions;
                return true;
            case "Latest":
                result = MolangVersion.Latest;
                return true;
            case "HardcodedMolang":
                result = MolangVersion.HardcodedMolang;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

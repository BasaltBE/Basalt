namespace Basalt.BedrockProtocol.Enums;

public enum MolangVersion : short {
    Invalid = -1,
    BeforeVersioning,
    Initial,
    FixedItemRemainingUseDurationQuery,
    ExpressionErrorMessages,
    UnexpectedOperatorErrors,
    ConditionalOperatorAssociativity,
    ComparisonAndLogicalOperatorPrecedence,
    DivideByNegativeValue,
    FixedCapeFlapAmountQuery,
    QueryBlockPropertyRenamedToState,
    DeprecateOldBlockQueryNames,
    DeprecatedSnifferAndCamelQueries,
    LeafSupportingInFirstSolidBlockBelow,
    Latest = 13,
    HardcodedMolang = 13,
    NumValidVersions = 14
}

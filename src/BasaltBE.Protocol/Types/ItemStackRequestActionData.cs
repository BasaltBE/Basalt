using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public abstract class ItemStackRequestActionData : DataType {
    public abstract ItemStackRequestActionType ActionType { get; }

    public static ItemStackRequestActionData ReadAction(ref BinaryReader reader) {
        uint selector = reader.ReadVarUInt();
        ItemStackRequestActionType actionType = (ItemStackRequestActionType)reader.ReadUInt8();
        uint expectedSelector = actionType switch {
            ItemStackRequestActionType.Take => 0,
            ItemStackRequestActionType.Place => 1,
            ItemStackRequestActionType.Swap => 2,
            ItemStackRequestActionType.Drop => 3,
            ItemStackRequestActionType.Destroy => 4,
            ItemStackRequestActionType.Consume => 5,
            ItemStackRequestActionType.Create => 6,
            ItemStackRequestActionType.ScreenLabTableCombine => 7,
            ItemStackRequestActionType.ScreenBeaconPayment => 8,
            ItemStackRequestActionType.ScreenHudMineBlock => 9,
            ItemStackRequestActionType.CraftRecipe => 10,
            ItemStackRequestActionType.CraftRecipeAuto => 11,
            ItemStackRequestActionType.CraftCreative => 12,
            ItemStackRequestActionType.CraftRecipeOptional => 13,
            ItemStackRequestActionType.CraftRepairAndDisenchant => 14,
            ItemStackRequestActionType.CraftLoom => 15,
            ItemStackRequestActionType.CraftReservedAction => 16,
            ItemStackRequestActionType.CraftNonImplemented => 17,
            ItemStackRequestActionType.CraftResults => 18,
            _ => throw new InvalidDataException($"Unknown item stack request action type {(byte)actionType}.")
        };
        if (selector != expectedSelector)
            throw new InvalidDataException($"Item stack request action selector {selector} does not match action type {(byte)actionType}.");

        ItemStackRequestActionData action = actionType switch {
            ItemStackRequestActionType.Take => new TakeActionData(),
            ItemStackRequestActionType.Place => new PlaceActionData(),
            ItemStackRequestActionType.Swap => new SwapActionData(),
            ItemStackRequestActionType.Drop => new DropActionData(),
            ItemStackRequestActionType.Destroy => new DestroyActionData(),
            ItemStackRequestActionType.Consume => new ConsumeActionData(),
            ItemStackRequestActionType.Create => new CreateActionData(),
            ItemStackRequestActionType.ScreenLabTableCombine => new LabTableCombineActionData(),
            ItemStackRequestActionType.ScreenBeaconPayment => new BeaconPaymentActionData(),
            ItemStackRequestActionType.ScreenHudMineBlock => new MineBlockActionData(),
            ItemStackRequestActionType.CraftRecipe => new CraftRecipeActionData(),
            ItemStackRequestActionType.CraftRecipeAuto => new CraftRecipeAutoActionData(),
            ItemStackRequestActionType.CraftCreative => new CraftCreativeActionData(),
            ItemStackRequestActionType.CraftRecipeOptional => new CraftRecipeOptionalActionData(),
            ItemStackRequestActionType.CraftRepairAndDisenchant => new CraftRepairAndDisenchantActionData(),
            ItemStackRequestActionType.CraftLoom => new CraftLoomActionData(),
            ItemStackRequestActionType.CraftReservedAction => new CraftDiscPressActionData(),
            ItemStackRequestActionType.CraftNonImplemented => new CraftNonImplementedActionData(),
            ItemStackRequestActionType.CraftResults => new CraftResultsActionData(),
            _ => throw new InvalidDataException($"Unknown item stack request action type {(byte)actionType}.")
        };
        action.Read(ref reader);
        return action;
    }

    public void WriteAction(ref BinaryWriter writer) {
        uint selector = ActionType switch {
            ItemStackRequestActionType.Take => 0,
            ItemStackRequestActionType.Place => 1,
            ItemStackRequestActionType.Swap => 2,
            ItemStackRequestActionType.Drop => 3,
            ItemStackRequestActionType.Destroy => 4,
            ItemStackRequestActionType.Consume => 5,
            ItemStackRequestActionType.Create => 6,
            ItemStackRequestActionType.ScreenLabTableCombine => 7,
            ItemStackRequestActionType.ScreenBeaconPayment => 8,
            ItemStackRequestActionType.ScreenHudMineBlock => 9,
            ItemStackRequestActionType.CraftRecipe => 10,
            ItemStackRequestActionType.CraftRecipeAuto => 11,
            ItemStackRequestActionType.CraftCreative => 12,
            ItemStackRequestActionType.CraftRecipeOptional => 13,
            ItemStackRequestActionType.CraftRepairAndDisenchant => 14,
            ItemStackRequestActionType.CraftLoom => 15,
            ItemStackRequestActionType.CraftReservedAction => 16,
            ItemStackRequestActionType.CraftNonImplemented => 17,
            ItemStackRequestActionType.CraftResults => 18,
            _ => throw new InvalidDataException($"Unknown item stack request action type {(byte)ActionType}.")
        };
        writer.WriteVarUInt(selector);
        writer.WriteUInt8((byte)ActionType);
        Write(ref writer);
    }
}

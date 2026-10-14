using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public abstract class ItemStackRequestActionData : DataType {
    public abstract ItemStackRequestActionType ActionType { get; }

    public static ItemStackRequestActionData ReadAction(ref BinaryReader reader) {
        ItemStackRequestActionType actionType = (ItemStackRequestActionType)reader.ReadUInt8();
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
        writer.WriteUInt8((byte)ActionType);
        Write(ref writer);
    }
}

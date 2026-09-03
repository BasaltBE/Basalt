using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ItemStackRequestAction : DataType {
    public ItemStackRequestActionType Type;
    public byte Amount;
    public SlotInfoData Source = new();
    public SlotInfoData Destination = new();
    public bool Randomly;
    public byte ResultsIndex;
    public int PrimaryEffectId;
    public int SecondaryEffectId;
    public int Slot;
    public int PredictedDurability;
    public int NetIdVariant;
    public uint RecipeNetId;
    public byte NumberOfRequestedCrafts;
    public RecipeIngredientData[] Ingredients = [];
    public uint CreativeItemNetId;
    public int FilteredStringIndex;
    public int RepairCost;
    public string PatternNameId = string.Empty;
    public NetworkItemInstanceDescriptorData[] CraftResults = [];
    public byte NumCrafts;

    public override void Write(ref BinaryWriter writer) {
        uint selector = Type switch {
            ItemStackRequestActionType.Take => 0,
            ItemStackRequestActionType.Place => 1,
            ItemStackRequestActionType.Swap => 2,
            ItemStackRequestActionType.Drop => 3,
            ItemStackRequestActionType.Destroy => 4,
            ItemStackRequestActionType.Consume => 5,
            ItemStackRequestActionType.Create => 6,
            ItemStackRequestActionType.PlaceInItemContainer => 1,
            ItemStackRequestActionType.TakeFromItemContainer => 0,
            ItemStackRequestActionType.ScreenLabTableCombine => 7,
            ItemStackRequestActionType.ScreenBeaconPayment => 8,
            ItemStackRequestActionType.ScreenHUDMineBlock => 9,
            ItemStackRequestActionType.CraftRecipe => 10,
            ItemStackRequestActionType.CraftRecipeAuto => 11,
            ItemStackRequestActionType.CraftCreative => 12,
            ItemStackRequestActionType.CraftRecipeOptional => 13,
            ItemStackRequestActionType.CraftRepairAndDisenchant => 14,
            ItemStackRequestActionType.CraftLoom => 15,
            ItemStackRequestActionType.CraftNonImplemented => 16,
            ItemStackRequestActionType.CraftResults => 17,
            _ => throw new ArgumentOutOfRangeException(nameof(Type))
        };
        writer.WriteVarUInt(selector);
        writer.WriteUInt8((byte)Type);
        switch (Type) {
            case ItemStackRequestActionType.Take:
            case ItemStackRequestActionType.TakeFromItemContainer:
            case ItemStackRequestActionType.Place:
            case ItemStackRequestActionType.PlaceInItemContainer:
                writer.WriteUInt8(Amount);
                Source.Write(ref writer);
                Destination.Write(ref writer);
                break;
            case ItemStackRequestActionType.Swap:
                Source.Write(ref writer);
                Destination.Write(ref writer);
                break;
            case ItemStackRequestActionType.Drop:
                writer.WriteUInt8(Amount);
                Source.Write(ref writer);
                writer.WriteBool(Randomly);
                break;
            case ItemStackRequestActionType.Destroy:
            case ItemStackRequestActionType.Consume:
                writer.WriteUInt8(Amount);
                Source.Write(ref writer);
                break;
            case ItemStackRequestActionType.Create:
                writer.WriteUInt8(ResultsIndex);
                break;
            case ItemStackRequestActionType.ScreenLabTableCombine:
            case ItemStackRequestActionType.CraftNonImplemented:
                break;
            case ItemStackRequestActionType.ScreenBeaconPayment:
                writer.WriteVarInt(PrimaryEffectId);
                writer.WriteVarInt(SecondaryEffectId);
                break;
            case ItemStackRequestActionType.ScreenHUDMineBlock:
                writer.WriteVarInt(Slot);
                writer.WriteVarInt(PredictedDurability);
                writer.WriteInt32(NetIdVariant, true);
                break;
            case ItemStackRequestActionType.CraftRecipe:
                writer.WriteVarUInt(RecipeNetId);
                writer.WriteUInt8(NumberOfRequestedCrafts);
                break;
            case ItemStackRequestActionType.CraftRecipeAuto:
                writer.WriteVarUInt(RecipeNetId);
                writer.WriteUInt8(NumberOfRequestedCrafts);
                writer.WriteVarUInt((uint)Ingredients.Length);
                foreach (RecipeIngredientData ingredient in Ingredients) ingredient.Write(ref writer);
                break;
            case ItemStackRequestActionType.CraftCreative:
                writer.WriteVarUInt(CreativeItemNetId);
                writer.WriteUInt8(NumberOfRequestedCrafts);
                break;
            case ItemStackRequestActionType.CraftRecipeOptional:
                writer.WriteVarUInt(RecipeNetId);
                writer.WriteInt32(FilteredStringIndex, true);
                break;
            case ItemStackRequestActionType.CraftRepairAndDisenchant:
                writer.WriteInt32(unchecked((int)RecipeNetId), true);
                writer.WriteUInt8(NumberOfRequestedCrafts);
                writer.WriteVarInt(RepairCost);
                break;
            case ItemStackRequestActionType.CraftLoom:
                writer.WriteVarString(PatternNameId);
                writer.WriteUInt8(NumCrafts);
                break;
            case ItemStackRequestActionType.CraftResults:
                writer.WriteVarUInt((uint)CraftResults.Length);
                foreach (NetworkItemInstanceDescriptorData result in CraftResults) result.Write(ref writer);
                writer.WriteUInt8(NumCrafts);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Type));
        }
    }

    public override void Read(ref BinaryReader reader) {
        reader.ReadVarUInt();
        Type = (ItemStackRequestActionType)reader.ReadUInt8();
        switch (Type) {
            case ItemStackRequestActionType.Take:
            case ItemStackRequestActionType.TakeFromItemContainer:
            case ItemStackRequestActionType.Place:
            case ItemStackRequestActionType.PlaceInItemContainer:
                Amount = reader.ReadUInt8();
                Source.Read(ref reader);
                Destination.Read(ref reader);
                break;
            case ItemStackRequestActionType.Swap:
                Source.Read(ref reader);
                Destination.Read(ref reader);
                break;
            case ItemStackRequestActionType.Drop:
                Amount = reader.ReadUInt8();
                Source.Read(ref reader);
                Randomly = reader.ReadBool();
                break;
            case ItemStackRequestActionType.Destroy:
            case ItemStackRequestActionType.Consume:
                Amount = reader.ReadUInt8();
                Source.Read(ref reader);
                break;
            case ItemStackRequestActionType.Create:
                ResultsIndex = reader.ReadUInt8();
                break;
            case ItemStackRequestActionType.ScreenLabTableCombine:
            case ItemStackRequestActionType.CraftNonImplemented:
                break;
            case ItemStackRequestActionType.ScreenBeaconPayment:
                PrimaryEffectId = reader.ReadVarInt();
                SecondaryEffectId = reader.ReadVarInt();
                break;
            case ItemStackRequestActionType.ScreenHUDMineBlock:
                Slot = reader.ReadVarInt();
                PredictedDurability = reader.ReadVarInt();
                NetIdVariant = reader.ReadInt32(true);
                break;
            case ItemStackRequestActionType.CraftRecipe:
                RecipeNetId = reader.ReadVarUInt();
                NumberOfRequestedCrafts = reader.ReadUInt8();
                break;
            case ItemStackRequestActionType.CraftRecipeAuto:
                RecipeNetId = reader.ReadVarUInt();
                NumberOfRequestedCrafts = reader.ReadUInt8();
                Ingredients = new RecipeIngredientData[checked((int)reader.ReadVarUInt())];
                for (int index = 0; index < Ingredients.Length; index++) {
                    RecipeIngredientData ingredient = new();
                    ingredient.Read(ref reader);
                    Ingredients[index] = ingredient;
                }
                break;
            case ItemStackRequestActionType.CraftCreative:
                CreativeItemNetId = reader.ReadVarUInt();
                NumberOfRequestedCrafts = reader.ReadUInt8();
                break;
            case ItemStackRequestActionType.CraftRecipeOptional:
                RecipeNetId = reader.ReadVarUInt();
                FilteredStringIndex = reader.ReadInt32(true);
                break;
            case ItemStackRequestActionType.CraftRepairAndDisenchant:
                RecipeNetId = unchecked((uint)reader.ReadInt32(true));
                NumberOfRequestedCrafts = reader.ReadUInt8();
                RepairCost = reader.ReadVarInt();
                break;
            case ItemStackRequestActionType.CraftLoom:
                PatternNameId = reader.ReadVarString();
                NumCrafts = reader.ReadUInt8();
                break;
            case ItemStackRequestActionType.CraftResults:
                CraftResults = new NetworkItemInstanceDescriptorData[checked((int)reader.ReadVarUInt())];
                for (int index = 0; index < CraftResults.Length; index++) {
                    NetworkItemInstanceDescriptorData result = new();
                    result.Read(ref reader);
                    CraftResults[index] = result;
                }
                NumCrafts = reader.ReadUInt8();
                break;
            default:
                throw new FormatException("Unsupported item stack request action type.");
        }
    }
}

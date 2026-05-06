using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public struct Vec2f
{
    public float X { get; set; }
    public float Y { get; set; }

    public void Read(ref BinaryReader reader)
    {
        X = reader.ReadF32(true);
        Y = reader.ReadF32(true);
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteF32(X, true);
        writer.WriteF32(Y, true);
    }
}

public sealed class ItemStack
{
    public int NetworkId { get; set; }
    public ushort Count { get; set; }
    public uint MetadataValue { get; set; }
    public int BlockRuntimeId { get; set; }
    public byte[] ExtraData { get; set; } = [];

    public void Read(ref BinaryReader reader)
    {
        NetworkId = reader.ReadZigZag();
        if (NetworkId == 0)
        {
            Count = 0;
            MetadataValue = 0;
            BlockRuntimeId = 0;
            ExtraData = [];
            return;
        }

        Count = reader.ReadUInt16(true);
        MetadataValue = reader.ReadVarUInt();
        BlockRuntimeId = reader.ReadZigZag();
        ExtraData = reader.ReadBytes(checked((int)reader.ReadVarUInt())).ToArray();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteZigZag(NetworkId);
        if (NetworkId == 0)
        {
            return;
        }

        writer.WriteUInt16(Count, true);
        writer.WriteVarUInt(MetadataValue);
        writer.WriteZigZag(BlockRuntimeId);
        writer.WriteVarUInt((uint)ExtraData.Length);
        writer.WriteBytes(ExtraData);
    }
}

public sealed class ItemInstance
{
    public ItemStack Stack { get; set; } = new();
    public int StackNetworkId { get; set; }

    public void Read(ref BinaryReader reader)
    {
        Stack.NetworkId = reader.ReadZigZag();
        if (Stack.NetworkId == 0)
        {
            Stack.Count = 0;
            Stack.MetadataValue = 0;
            Stack.BlockRuntimeId = 0;
            Stack.ExtraData = [];
            StackNetworkId = 0;
            return;
        }

        Stack.Count = reader.ReadUInt16(true);
        Stack.MetadataValue = reader.ReadVarUInt();
        bool hasNetId = reader.ReadBool();
        StackNetworkId = hasNetId ? reader.ReadZigZag() : 0;
        Stack.BlockRuntimeId = reader.ReadZigZag();
        Stack.ExtraData = reader.ReadBytes(checked((int)reader.ReadVarUInt())).ToArray();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteZigZag(Stack.NetworkId);
        if (Stack.NetworkId == 0)
        {
            return;
        }

        writer.WriteUInt16(Stack.Count, true);
        writer.WriteVarUInt(Stack.MetadataValue);
        bool hasNetId = StackNetworkId != 0;
        writer.WriteBool(hasNetId);
        if (hasNetId)
        {
            writer.WriteZigZag(StackNetworkId);
        }

        writer.WriteZigZag(Stack.BlockRuntimeId);
        writer.WriteVarUInt((uint)Stack.ExtraData.Length);
        writer.WriteBytes(Stack.ExtraData);
    }
}

public sealed class FullContainerName
{
    public byte ContainerId { get; set; }
    public OptionalValue<uint> DynamicContainerId { get; set; } = new();

    public void Read(ref BinaryReader reader)
    {
        ContainerId = reader.ReadUInt8();
        DynamicContainerId.Read(ref reader, static (ref BinaryReader r) => r.ReadUInt32(true));
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteUInt8(ContainerId);
        DynamicContainerId.Write(ref writer, static (ref BinaryWriter w, uint value) => w.WriteUInt32(value, true));
    }
}

public sealed class StackRequestSlotInfo
{
    public FullContainerName Container { get; set; } = new();
    public byte Slot { get; set; }
    public int StackNetworkId { get; set; }

    public void Read(ref BinaryReader reader)
    {
        Container.Read(ref reader);
        Slot = reader.ReadUInt8();
        StackNetworkId = reader.ReadZigZag();
    }

    public void Write(ref BinaryWriter writer)
    {
        Container.Write(ref writer);
        writer.WriteUInt8(Slot);
        writer.WriteZigZag(StackNetworkId);
    }
}

public interface IStackRequestAction
{
    byte ActionType { get; }
    void Read(ref BinaryReader reader);
    void Write(ref BinaryWriter writer);
}

public sealed class RawStackRequestAction : IStackRequestAction
{
    public byte Type { get; set; }
    public byte[] Data { get; set; } = [];
    public byte ActionType => Type;
    public void Read(ref BinaryReader reader) => throw new InvalidOperationException();
    public void Write(ref BinaryWriter writer) => writer.WriteBytes(Data);
}

public sealed class ItemDescriptorCount
{
    public byte DescriptorType { get; set; }
    public short NetworkId { get; set; }
    public short MetadataValue { get; set; }
    public string Text { get; set; } = string.Empty;
    public byte Version { get; set; }
    public int Count { get; set; }

    public void Read(ref BinaryReader reader)
    {
        DescriptorType = reader.ReadUInt8();
        switch (DescriptorType)
        {
            case 1:
                NetworkId = reader.ReadInt16(true);
                if (NetworkId != 0)
                {
                    MetadataValue = reader.ReadInt16(true);
                }
                break;
            case 2:
                Text = reader.ReadVarString();
                Version = reader.ReadUInt8();
                break;
            case 3:
            case 5:
                Text = reader.ReadVarString();
                break;
            case 4:
                Text = reader.ReadVarString();
                MetadataValue = reader.ReadInt16(true);
                break;
        }

        Count = reader.ReadZigZag();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteUInt8(DescriptorType);
        switch (DescriptorType)
        {
            case 1:
                writer.WriteInt16(NetworkId, true);
                if (NetworkId != 0)
                {
                    writer.WriteInt16(MetadataValue, true);
                }
                break;
            case 2:
                writer.WriteVarString(Text);
                writer.WriteUInt8(Version);
                break;
            case 3:
            case 5:
                writer.WriteVarString(Text);
                break;
            case 4:
                writer.WriteVarString(Text);
                writer.WriteInt16(MetadataValue, true);
                break;
        }

        writer.WriteZigZag(Count);
    }
}

public sealed class ItemStackRequest
{
    public int RequestId { get; set; }
    public List<IStackRequestAction> Actions { get; set; } = [];
    public List<string> FilterStrings { get; set; } = [];
    public int FilterCause { get; set; }

    public void Read(ref BinaryReader reader)
    {
        RequestId = reader.ReadZigZag();

        int actionCount = checked((int)reader.ReadVarUInt());
        Actions = new(actionCount);
        for (int i = 0; i < actionCount; i++)
        {
            byte type = reader.ReadUInt8();
            IStackRequestAction action = StackRequestActions.Create(type);
            action.Read(ref reader);
            Actions.Add(action);
        }

        int filterCount = checked((int)reader.ReadVarUInt());
        FilterStrings = new(filterCount);
        for (int i = 0; i < filterCount; i++)
        {
            FilterStrings.Add(reader.ReadVarString());
        }

        FilterCause = reader.ReadInt32(true);
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteZigZag(RequestId);
        writer.WriteVarUInt((uint)Actions.Count);
        for (int i = 0; i < Actions.Count; i++)
        {
            writer.WriteUInt8(Actions[i].ActionType);
            Actions[i].Write(ref writer);
        }

        writer.WriteVarUInt((uint)FilterStrings.Count);
        for (int i = 0; i < FilterStrings.Count; i++)
        {
            writer.WriteVarString(FilterStrings[i]);
        }

        writer.WriteInt32(FilterCause, true);
    }
}

public sealed class LegacySetItemSlot
{
    public byte ContainerId { get; set; }
    public byte[] Slots { get; set; } = [];

    public void Read(ref BinaryReader reader)
    {
        ContainerId = reader.ReadUInt8();
        Slots = reader.ReadBytes(checked((int)reader.ReadVarUInt())).ToArray();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteUInt8(ContainerId);
        writer.WriteVarUInt((uint)Slots.Length);
        writer.WriteBytes(Slots);
    }
}

public sealed class UseItemTransactionData
{
    public int LegacyRequestId { get; set; }
    public List<LegacySetItemSlot> LegacySetItemSlots { get; set; } = [];
    public List<InventoryAction> Actions { get; set; } = [];
    public uint ActionType { get; set; }
    public uint TriggerType { get; set; }
    public BlockPos BlockPosition { get; set; }
    public int BlockFace { get; set; }
    public int HotBarSlot { get; set; }
    public ItemInstance HeldItem { get; set; } = new();
    public Vec3f Position { get; set; }
    public Vec3f ClickedPosition { get; set; }
    public uint BlockRuntimeId { get; set; }
    public uint ClientPrediction { get; set; }
    public byte ClientCooldownState { get; set; }

    public void Read(ref BinaryReader reader)
    {
        LegacyRequestId = reader.ReadZigZag();
        if (LegacyRequestId < -1 && (LegacyRequestId & 1) == 0)
        {
            int legacyCount = checked((int)reader.ReadVarUInt());
            LegacySetItemSlots = new(legacyCount);
            for (int i = 0; i < legacyCount; i++)
            {
                LegacySetItemSlot slot = new();
                slot.Read(ref reader);
                LegacySetItemSlots.Add(slot);
            }
        }

        int actionCount = checked((int)reader.ReadVarUInt());
        Actions = new(actionCount);
        for (int i = 0; i < actionCount; i++)
        {
            InventoryAction action = new();
            action.Read(ref reader);
            Actions.Add(action);
        }

        ActionType = reader.ReadVarUInt();
        TriggerType = reader.ReadVarUInt();
        BlockPosition.Read(ref reader);
        BlockFace = reader.ReadZigZag();
        HotBarSlot = reader.ReadZigZag();
        HeldItem.Read(ref reader);
        Position.Read(ref reader);
        ClickedPosition.Read(ref reader);
        BlockRuntimeId = reader.ReadVarUInt();
        ClientPrediction = reader.ReadVarUInt();
        ClientCooldownState = reader.ReadUInt8();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteZigZag(LegacyRequestId);
        if (LegacyRequestId < -1 && (LegacyRequestId & 1) == 0)
        {
            writer.WriteVarUInt((uint)LegacySetItemSlots.Count);
            for (int i = 0; i < LegacySetItemSlots.Count; i++)
            {
                LegacySetItemSlots[i].Write(ref writer);
            }
        }

        writer.WriteVarUInt((uint)Actions.Count);
        for (int i = 0; i < Actions.Count; i++)
        {
            Actions[i].Write(ref writer);
        }

        writer.WriteVarUInt(ActionType);
        writer.WriteVarUInt(TriggerType);
        BlockPosition.Write(ref writer);
        writer.WriteZigZag(BlockFace);
        writer.WriteZigZag(HotBarSlot);
        HeldItem.Write(ref writer);
        Position.Write(ref writer);
        ClickedPosition.Write(ref writer);
        writer.WriteVarUInt(BlockRuntimeId);
        writer.WriteVarUInt(ClientPrediction);
        writer.WriteUInt8(ClientCooldownState);
    }
}

public sealed class InventoryAction
{
    public uint SourceType { get; set; }
    public int WindowId { get; set; }
    public uint SourceFlags { get; set; }
    public uint InventorySlot { get; set; }
    public ItemInstance OldItem { get; set; } = new();
    public ItemInstance NewItem { get; set; } = new();

    public void Read(ref BinaryReader reader)
    {
        SourceType = reader.ReadVarUInt();
        if (SourceType == 0 || SourceType == 99999)
        {
            WindowId = reader.ReadZigZag();
        }
        else if (SourceType == 2)
        {
            SourceFlags = reader.ReadVarUInt();
        }

        InventorySlot = reader.ReadVarUInt();
        OldItem.Read(ref reader);
        NewItem.Read(ref reader);
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarUInt(SourceType);
        if (SourceType == 0 || SourceType == 99999)
        {
            writer.WriteZigZag(WindowId);
        }
        else if (SourceType == 2)
        {
            writer.WriteVarUInt(SourceFlags);
        }

        writer.WriteVarUInt(InventorySlot);
        OldItem.Write(ref writer);
        NewItem.Write(ref writer);
    }
}

public static class StackRequestActions
{
    public static IStackRequestAction Create(byte type) => type switch
    {
        0 => new TransferStackRequestAction(type),
        1 => new TransferStackRequestAction(type),
        2 => new SwapStackRequestAction(),
        3 => new DropStackRequestAction(),
        4 => new DestroyStackRequestAction(type),
        5 => new DestroyStackRequestAction(type),
        6 => new CreateStackRequestAction(),
        7 => new TransferStackRequestAction(type),
        8 => new TransferStackRequestAction(type),
        9 => new EmptyStackRequestAction(type),
        10 => new BeaconPaymentStackRequestAction(),
        11 => new MineBlockStackRequestAction(),
        12 => new CraftRecipeStackRequestAction(),
        13 => new AutoCraftRecipeStackRequestAction(),
        14 => new CraftCreativeStackRequestAction(),
        15 => new CraftRecipeOptionalStackRequestAction(),
        16 => new CraftGrindstoneRecipeStackRequestAction(),
        17 => new CraftLoomRecipeStackRequestAction(),
        18 => new EmptyStackRequestAction(type),
        19 => new CraftResultsDeprecatedStackRequestAction(),
        _ => new RawStackRequestAction { Type = type }
    };
}

public sealed class EmptyStackRequestAction(byte type) : IStackRequestAction
{
    public byte ActionType => type;
    public void Read(ref BinaryReader reader) {}
    public void Write(ref BinaryWriter writer) {}
}

public sealed class TransferStackRequestAction(byte type) : IStackRequestAction
{
    public byte ActionType => type;
    public byte Count { get; set; }
    public StackRequestSlotInfo Source { get; set; } = new();
    public StackRequestSlotInfo Destination { get; set; } = new();

    public void Read(ref BinaryReader reader)
    {
        Count = reader.ReadUInt8();
        Source.Read(ref reader);
        Destination.Read(ref reader);
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteUInt8(Count);
        Source.Write(ref writer);
        Destination.Write(ref writer);
    }
}

public sealed class SwapStackRequestAction : IStackRequestAction
{
    public byte ActionType => 2;
    public StackRequestSlotInfo Source { get; set; } = new();
    public StackRequestSlotInfo Destination { get; set; } = new();

    public void Read(ref BinaryReader reader)
    {
        Source.Read(ref reader);
        Destination.Read(ref reader);
    }

    public void Write(ref BinaryWriter writer)
    {
        Source.Write(ref writer);
        Destination.Write(ref writer);
    }
}

public sealed class DropStackRequestAction : IStackRequestAction
{
    public byte ActionType => 3;
    public byte Count { get; set; }
    public StackRequestSlotInfo Source { get; set; } = new();
    public bool Randomly { get; set; }

    public void Read(ref BinaryReader reader)
    {
        Count = reader.ReadUInt8();
        Source.Read(ref reader);
        Randomly = reader.ReadBool();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteUInt8(Count);
        Source.Write(ref writer);
        writer.WriteBool(Randomly);
    }
}

public sealed class DestroyStackRequestAction(byte type) : IStackRequestAction
{
    public byte ActionType => type;
    public byte Count { get; set; }
    public StackRequestSlotInfo Source { get; set; } = new();

    public void Read(ref BinaryReader reader)
    {
        Count = reader.ReadUInt8();
        Source.Read(ref reader);
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteUInt8(Count);
        Source.Write(ref writer);
    }
}

public sealed class CreateStackRequestAction : IStackRequestAction
{
    public byte ActionType => 6;
    public byte ResultsSlot { get; set; }
    public void Read(ref BinaryReader reader) => ResultsSlot = reader.ReadUInt8();
    public void Write(ref BinaryWriter writer) => writer.WriteUInt8(ResultsSlot);
}

public sealed class BeaconPaymentStackRequestAction : IStackRequestAction
{
    public byte ActionType => 10;
    public int PrimaryEffect { get; set; }
    public int SecondaryEffect { get; set; }
    public void Read(ref BinaryReader reader)
    {
        PrimaryEffect = reader.ReadZigZag();
        SecondaryEffect = reader.ReadZigZag();
    }
    public void Write(ref BinaryWriter writer)
    {
        writer.WriteZigZag(PrimaryEffect);
        writer.WriteZigZag(SecondaryEffect);
    }
}

public sealed class MineBlockStackRequestAction : IStackRequestAction
{
    public byte ActionType => 11;
    public int HotbarSlot { get; set; }
    public int PredictedDurability { get; set; }
    public int StackNetworkId { get; set; }
    public void Read(ref BinaryReader reader)
    {
        HotbarSlot = reader.ReadZigZag();
        PredictedDurability = reader.ReadZigZag();
        StackNetworkId = reader.ReadZigZag();
    }
    public void Write(ref BinaryWriter writer)
    {
        writer.WriteZigZag(HotbarSlot);
        writer.WriteZigZag(PredictedDurability);
        writer.WriteZigZag(StackNetworkId);
    }
}

public sealed class CraftRecipeStackRequestAction : IStackRequestAction
{
    public byte ActionType => 12;
    public uint RecipeNetworkId { get; set; }
    public byte NumberOfCrafts { get; set; }
    public void Read(ref BinaryReader reader)
    {
        RecipeNetworkId = reader.ReadVarUInt();
        NumberOfCrafts = reader.ReadUInt8();
    }
    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarUInt(RecipeNetworkId);
        writer.WriteUInt8(NumberOfCrafts);
    }
}

public sealed class AutoCraftRecipeStackRequestAction : IStackRequestAction
{
    public byte ActionType => 13;
    public uint RecipeNetworkId { get; set; }
    public byte NumberOfCrafts { get; set; }
    public byte TimesCrafted { get; set; }
    public List<ItemDescriptorCount> Ingredients { get; set; } = [];

    public void Read(ref BinaryReader reader)
    {
        RecipeNetworkId = reader.ReadVarUInt();
        NumberOfCrafts = reader.ReadUInt8();
        TimesCrafted = reader.ReadUInt8();
        int ingredientCount = checked((int)reader.ReadVarUInt());
        Ingredients = new(ingredientCount);
        for (int i = 0; i < ingredientCount; i++)
        {
            ItemDescriptorCount ingredient = new();
            ingredient.Read(ref reader);
            Ingredients.Add(ingredient);
        }
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarUInt(RecipeNetworkId);
        writer.WriteUInt8(NumberOfCrafts);
        writer.WriteUInt8(TimesCrafted);
        writer.WriteVarUInt((uint)Ingredients.Count);
        for (int i = 0; i < Ingredients.Count; i++)
        {
            Ingredients[i].Write(ref writer);
        }
    }
}

public sealed class CraftCreativeStackRequestAction : IStackRequestAction
{
    public byte ActionType => 14;
    public uint CreativeItemNetworkId { get; set; }
    public byte NumberOfCrafts { get; set; }
    public void Read(ref BinaryReader reader)
    {
        CreativeItemNetworkId = reader.ReadVarUInt();
        NumberOfCrafts = reader.ReadUInt8();
    }
    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarUInt(CreativeItemNetworkId);
        writer.WriteUInt8(NumberOfCrafts);
    }
}

public sealed class CraftRecipeOptionalStackRequestAction : IStackRequestAction
{
    public byte ActionType => 15;
    public uint RecipeNetworkId { get; set; }
    public int FilterStringIndex { get; set; }
    public void Read(ref BinaryReader reader)
    {
        RecipeNetworkId = reader.ReadVarUInt();
        FilterStringIndex = reader.ReadInt32(true);
    }
    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarUInt(RecipeNetworkId);
        writer.WriteInt32(FilterStringIndex, true);
    }
}

public sealed class CraftGrindstoneRecipeStackRequestAction : IStackRequestAction
{
    public byte ActionType => 16;
    public uint RecipeNetworkId { get; set; }
    public byte NumberOfCrafts { get; set; }
    public int Cost { get; set; }
    public void Read(ref BinaryReader reader)
    {
        RecipeNetworkId = reader.ReadVarUInt();
        NumberOfCrafts = reader.ReadUInt8();
        Cost = reader.ReadZigZag();
    }
    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarUInt(RecipeNetworkId);
        writer.WriteUInt8(NumberOfCrafts);
        writer.WriteZigZag(Cost);
    }
}

public sealed class CraftLoomRecipeStackRequestAction : IStackRequestAction
{
    public byte ActionType => 17;
    public string Pattern { get; set; } = string.Empty;
    public byte TimesCrafted { get; set; }
    public void Read(ref BinaryReader reader)
    {
        Pattern = reader.ReadVarString();
        TimesCrafted = reader.ReadUInt8();
    }
    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarString(Pattern);
        writer.WriteUInt8(TimesCrafted);
    }
}

public sealed class CraftResultsDeprecatedStackRequestAction : IStackRequestAction
{
    public byte ActionType => 19;
    public List<ItemStack> ResultItems { get; set; } = [];
    public byte TimesCrafted { get; set; }

    public void Read(ref BinaryReader reader)
    {
        int count = checked((int)reader.ReadVarUInt());
        ResultItems = new(count);
        for (int i = 0; i < count; i++)
        {
            ItemStack item = new();
            item.Read(ref reader);
            ResultItems.Add(item);
        }

        TimesCrafted = reader.ReadUInt8();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarUInt((uint)ResultItems.Count);
        for (int i = 0; i < ResultItems.Count; i++)
        {
            ResultItems[i].Write(ref writer);
        }

        writer.WriteUInt8(TimesCrafted);
    }
}

public sealed class PlayerBlockAction
{
    public int Action { get; set; }
    public BlockPos BlockPos { get; set; }
    public int Face { get; set; }

    public void Read(ref BinaryReader reader)
    {
        Action = reader.ReadZigZag();
        if (Action is 0 or 1 or 18 or 26 or 27)
        {
            BlockPos.Read(ref reader);
            Face = reader.ReadZigZag();
        }
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteZigZag(Action);
        if (Action is 0 or 1 or 18 or 26 or 27)
        {
            BlockPos.Write(ref writer);
            writer.WriteZigZag(Face);
        }
    }
}


namespace Basalt.Protocol.Types;

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

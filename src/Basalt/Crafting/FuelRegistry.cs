namespace Basalt.Core.Crafting;

using Basalt.Core.Item;

// For now fuel is hard coded as we have no way of dumping them
public static class FuelRegistry {
    private static readonly Dictionary<string, int> BurnTimes = new(StringComparer.Ordinal);

    static FuelRegistry() {
        // Coal and charcoal.
        Register(ItemIdentifier.Coal, 1600);
        Register(ItemIdentifier.Charcoal, 1600);
        Register(ItemIdentifier.CoalBlock, 16000);

        // Wood-based items.
        Register(ItemIdentifier.OakPlanks, 300);
        Register(ItemIdentifier.SprucePlanks, 300);
        Register(ItemIdentifier.BirchPlanks, 300);
        Register(ItemIdentifier.JunglePlanks, 300);
        Register(ItemIdentifier.AcaciaPlanks, 300);
        Register(ItemIdentifier.DarkOakPlanks, 300);
        Register(ItemIdentifier.MangrovePlanks, 300);
        Register(ItemIdentifier.CherryPlanks, 300);
        Register(ItemIdentifier.CrimsonPlanks, 300);
        Register(ItemIdentifier.WarpedPlanks, 300);

        // Logs and wood.
        Register(ItemIdentifier.OakLog, 300);
        Register(ItemIdentifier.SpruceLog, 300);
        Register(ItemIdentifier.BirchLog, 300);
        Register(ItemIdentifier.JungleLog, 300);
        Register(ItemIdentifier.AcaciaLog, 300);
        Register(ItemIdentifier.DarkOakLog, 300);
        Register(ItemIdentifier.MangroveLog, 300);
        Register(ItemIdentifier.CherryLog, 300);

        // Sticks.
        Register(ItemIdentifier.Stick, 100);

        // Tools and weapons.
        Register(ItemIdentifier.WoodenSword, 200);
        Register(ItemIdentifier.WoodenPickaxe, 200);
        Register(ItemIdentifier.WoodenAxe, 200);
        Register(ItemIdentifier.WoodenShovel, 200);
        Register(ItemIdentifier.WoodenHoe, 200);

        // Misc.
        Register(ItemIdentifier.Bamboo, 50);
        Register(ItemIdentifier.DriedKelpBlock, 4001);
        Register(ItemIdentifier.BlazeRod, 2400);
        Register(ItemIdentifier.LavaBucket, 20000);
        Register(ItemIdentifier.Bow, 300);
        Register(ItemIdentifier.FishingRod, 300);

        // Slabs (half-wood).
        Register(ItemIdentifier.OakSlab, 150);
        Register(ItemIdentifier.SpruceSlab, 150);
        Register(ItemIdentifier.BirchSlab, 150);
        Register(ItemIdentifier.JungleSlab, 150);
        Register(ItemIdentifier.AcaciaSlab, 150);
        Register(ItemIdentifier.DarkOakSlab, 150);

        // Wool.
        Register(ItemIdentifier.WhiteWool, 100);

        // Carpet.
        Register(ItemIdentifier.WhiteCarpet, 67);

        // Misc wood items.
        Register(ItemIdentifier.Ladder, 300);
        Register(ItemIdentifier.CraftingTable, 300);
        Register(ItemIdentifier.Bookshelf, 300);
        Register(ItemIdentifier.Chest, 300);
    }

    private static void Register(ItemIdentifier item, int burnTicks) {
        string identifier = item.ToIdentifier();
        BurnTimes[identifier] = burnTicks;
    }

    public static int GetBurnTime(ItemStack item) {
        return BurnTimes.TryGetValue(item.Type.Identifier, out int ticks) ? ticks : 0;
    }

    public static int GetBurnTime(string identifier) {
        return BurnTimes.TryGetValue(identifier, out int ticks) ? ticks : 0;
    }

    public static bool IsFuel(ItemStack item) {
        return BurnTimes.ContainsKey(item.Type.Identifier);
    }
}

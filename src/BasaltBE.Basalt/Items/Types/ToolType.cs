namespace BasaltBE.Basalt.Items.Types;

[Flags]
public enum ToolType : byte {
    None = 0,
    Pickaxe = 1,
    Axe = 2,
    Shovel = 4,
    Hoe = 8,
    Sword = 16
}

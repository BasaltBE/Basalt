namespace Basalt.Core.Blocks.Traits;

using System.Reflection;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using BedrockProtocol.Nbt;

public abstract class BlockTrait {
    public static readonly string[] Types = [];
    public static readonly string[] Tags = [];
    public static readonly Type? Component;
    public static readonly Type[] Components = [];

    protected Basalt.Core.Blocks.Block Block { get; }
    public virtual bool Interactable => false;
    public virtual string Identifier {
        get {
#pragma warning disable IL2075
            if (GetType().GetProperty("Identifier", BindingFlags.Public | BindingFlags.Static) is PropertyInfo property &&
                property.PropertyType == typeof(string) &&
                property.GetValue(null) is string identifier &&
                !string.IsNullOrWhiteSpace(identifier)) {
                return identifier;
            }

            return GetType().FullName ?? GetType().Name;
#pragma warning restore IL2075
        }
    }

    protected BlockTrait(Basalt.Core.Blocks.Block block) {
        Block = block;
    }

    public virtual void OnAdd() {
    }

    public virtual void OnRemove() {
    }

    public virtual void OnRead(CompoundTag tag) {
    }

    public virtual void OnWrite(CompoundTag tag) {
    }

    public virtual void OnPlace(BlockPlaceDetails details) {
    }

    public virtual void OnBreak(BlockBreakDetails details) {
    }

    /// <summary>
    /// An Override 
    /// </summary>
    public virtual List<Item.ItemStack>? GetCustomDrops(BlockPermutation permutation) {
        return null;
    }

    public virtual void OnInteract(BlockInteractDetails details) {
    }

    public virtual void OnTick(BlockTickDetails details) {
    }

    public virtual void OnRandomTick(BlockRandomTickDetails details) {
    }

    public virtual void OnLandOn(BlockLandOnDetails details) {
    }

    public virtual void OnRender(Player.Player player, int x, int y, int z) {
    }
}








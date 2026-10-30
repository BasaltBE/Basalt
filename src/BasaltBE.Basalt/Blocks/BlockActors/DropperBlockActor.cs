using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Items;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Blocks.BlockActors;

public sealed class DropperBlockActor(BlockActorContext context) : DispenserBlockActor(context, ContainerType.Dropper, "Dropper") {
    protected override bool Dispense(int slot) {
        if (facing < 0 || Container.GetItem(slot) is not { } item)
            return false;

        int targetX = Position.X + facingOffset.X;
        int targetY = Position.Y + facingOffset.Y;
        int targetZ = Position.Z + facingOffset.Z;
        if (Dimension.GetChunk(targetX >> 4, targetZ >> 4) is not { } chunk ||
            Dimension.GetBlockActor(chunk, targetX & 15, targetY, targetZ & 15) is not { Inventory: not null } target)
            return base.Dispense(slot);

        byte targetFace = BlockDirections.OppositeByFace[facing];
        if (ReferenceEquals(target.Inventory, Container) || target.InsertItem(item.Clone(1), targetFace) is not null)
            return false;

        Container.TakeItem(slot);
        return true;
    }
}

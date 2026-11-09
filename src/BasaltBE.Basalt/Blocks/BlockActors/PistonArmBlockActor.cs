using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Blocks.BlockActors;

public sealed class PistonArmBlockActor(BlockActorContext context) : BlockActor(context) {
    public byte State;
    public bool Sticky = context.Identifier == "minecraft:sticky_piston";

    public bool Extended => State == 2;
    public override string ActorIdentifier => "PistonArm";

    public override void OnRead(CompoundTag nbt) {
        State = nbt.Get<ByteTag>("State")?.Value ?? 0;
        Sticky |= nbt.Get<ByteTag>("Sticky")?.Value == 1;
    }

    public override void OnWrite(CompoundTag nbt) {
        nbt.Add("AttachedBlocks", new ListTag());
        nbt.Add("BlockEntityVersion", new IntTag { Value = 0 });
        nbt.Add("BreakBlocks", new ListTag());
        nbt.Add("LastProgress", new FloatTag { Value = 1 });
        nbt.Add("NewState", new ByteTag { Value = State });
        nbt.Add("Progress", new FloatTag { Value = 1 });
        nbt.Add("State", new ByteTag { Value = State });
        nbt.Add("Sticky", new ByteTag { Value = Sticky ? (byte)1 : (byte)0 });
        nbt.Add("isMovable", new ByteTag { Value = 1 });
    }

    public override bool CanRemove() => Dimension.CanRemovePistonBase(this);
    public override void OnRemove() => Dimension.BreakPistonBase(this);

    public void SetExtended(bool extended) {
        State = extended ? (byte)2 : (byte)0;
        Update();
    }
}

using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Blocks.BlockActors;

public sealed class ComparatorBlockActor(BlockActorContext context) : BlockActor(context) {
    public int OutputSignal;
    public override string ActorIdentifier => "Comparator";

    public override void OnRead(CompoundTag nbt) {
        OutputSignal = Math.Clamp(nbt.Get<IntTag>("OutputSignal")?.Value ?? 0, 0, 15);
    }

    public override void OnWrite(CompoundTag nbt) {
        nbt.Add("BlockEntityVersion", new IntTag { Value = 0 });
        nbt.Add("OutputSignal", new IntTag { Value = OutputSignal });
    }

    internal void SetOutputSignal(int signal) {
        if (OutputSignal == signal)
            return;

        OutputSignal = signal;
        MarkDirty();
    }
}

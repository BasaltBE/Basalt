using BasaltBE.Nbt.Types;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.BlockActors.Types;

public abstract class BlockActor(BlockActorContext context) {
    public readonly Dimension Dimension = context.Dimension;
    public readonly BlockPos Position = context.Position;
    public readonly string Identifier = context.Identifier;
    public readonly BlockPermutation Permutation = context.Permutation;
    public readonly CompoundTag Nbt = context.Nbt;
    public virtual string ActorIdentifier => Identifier;

    public abstract void OnRead(CompoundTag nbt);
    public abstract void OnWrite(CompoundTag nbt);
    public virtual void OnPlace() { }
    public virtual void OnInteract(Player player) { }
    public virtual void OnRemove() { }

    public void Update() {
        CompoundTag update = new();
        update.Add("id", new StringTag { Value = ActorIdentifier });
        update.Add("x", new IntTag { Value = Position.X });
        update.Add("y", new IntTag { Value = Position.Y });
        update.Add("z", new IntTag { Value = Position.Z });
        OnWrite(update);
        foreach ((string name, Tag tag) in update.Tags)
            Nbt.Add(name, tag);

        Dimension.Broadcast(new BlockActorDataPacket {
            Position = Position,
            ActorData = update
        }, Position.X >> 4, Position.Z >> 4);
    }
}

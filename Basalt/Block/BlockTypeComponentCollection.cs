namespace Basalt.Server.Block;

using Basalt.Protocol.Nbt;


public sealed class BlockTypeComponentCollection : CompoundTag
{
    public object Block { get; }

    public BlockTypeComponentCollection(BlockType block)
    {
        Block = block;
        Name = "components";
    }

    public BlockTypeComponentCollection(BlockPermutation block)
    {
        Block = block;
        Name = "components";
    }
}








using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.LootTables;

namespace BasaltBE.Basalt.Blocks;

public sealed class BlockRegistry(Server server)
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;

    public CustomBlock Create(string identifier)
    {
        if (Environment.CurrentManagedThreadId != ownerThread)
            throw new InvalidOperationException("Configure blocks on the server startup thread.");
        bool registered = BlockPalette.FindType(identifier) is not null;

        CustomBlock definition = BlockPalette.Create(identifier);
        if (!registered)
            server.LootTables.Register(identifier, new BlockDrop(identifier, 1, 1));
        return definition;
    }

    public BlockType? Find(string identifier) => BlockPalette.FindType(identifier);

    public BlockType Get(string identifier) => Find(identifier)
        ?? throw new KeyNotFoundException($"Block '{identifier}' was not found.");
}

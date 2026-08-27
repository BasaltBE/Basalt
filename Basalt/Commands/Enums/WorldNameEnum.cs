namespace Basalt.Core.Commands;

public sealed class WorldNameEnum : SoftEnum {
    public WorldNameEnum() : base("world_name") {
    }

    public override string[] GetOptions(Server server) {
        string worldsDirectory = server.Properties.WorldPath;
        if (string.IsNullOrWhiteSpace(worldsDirectory)) {
            worldsDirectory = "worlds";
        }

        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        foreach (Worlds.World world in server.Worlds) {
            names.Add(world.Name);
        }

        if (Directory.Exists(worldsDirectory)) {
            foreach (string directory in Directory.GetDirectories(worldsDirectory)) {
                string name = Path.GetFileName(directory);
                if (!string.IsNullOrWhiteSpace(name)) {
                    names.Add(name);
                }
            }
        }

        return [.. names];
    }
}

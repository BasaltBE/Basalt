using BasaltBE.Basalt.Worlds;

namespace BasaltBE.Basalt.Events.Worlds;

public sealed class WorldInitializeEvent(World world) : Event {
    public readonly World World = world;
}

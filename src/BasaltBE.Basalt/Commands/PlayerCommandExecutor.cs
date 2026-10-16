using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Commands;

public sealed class PlayerCommandExecutor : CommandExecutor
{
    public PlayerCommandExecutor(Player player) : base(
        (player ?? throw new ArgumentNullException(nameof(player))).Network.Server, player)
    {
    }

    public override bool Allows(string permission) =>
        Player is { Spawned: true, Disposed: false } player && player.Permissions.Allows(permission);
}

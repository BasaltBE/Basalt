using Basalt.Protocol.Enums;
using Basalt.World;

namespace Basalt.Core;

public static class OfflinePlayerActions
{
    public static bool TrySetGamemode(Basalt.World.World world, string username, Gamemode gamemode)
    {
        return world.PlayerProfiles.TryUpdateGamemode(username, gamemode);
    }

    public static bool TryGrantOperator(Basalt.World.World world, string username)
    {
        if (!world.PlayerProfiles.TryGetXuid(username, out string xuid))
        {
            return false;
        }

        world.Operators.AddOperator(xuid);
        return world.PlayerProfiles.TryUpdateOperator(username, true);
    }

    public static bool TryRevokeOperator(Basalt.World.World world, string username)
    {
        if (!world.PlayerProfiles.TryGetXuid(username, out string xuid))
        {
            return false;
        }

        world.Operators.RemoveOperator(xuid);
        return world.PlayerProfiles.TryUpdateOperator(username, false);
    }
}

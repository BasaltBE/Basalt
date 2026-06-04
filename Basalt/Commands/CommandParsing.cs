namespace Basalt.Server.Commands;

using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using DimensionInstance = Basalt.Server.World.Dimension.Dimension;
using EntityInstance = Basalt.Server.Entity.Entity;
using Player = global::Basalt.Server.Player.Player;
using ServerInstance = global::Basalt.Server.Server;
using WorldInstance = Basalt.Server.World.World;

public static class CommandParsing
{
    public static bool TryParsePositionComponent(string token, float origin, out float value)
    {
        value = 0f;
        if (token == "~")
        {
            value = origin;
            return true;
        }

        if (token.StartsWith('~'))
        {
            string offset = token[1..];
            if (offset.Length == 0)
            {
                value = origin;
                return true;
            }

            if (!float.TryParse(offset, out float step))
            {
                return false;
            }

            value = origin + step;
            return true;
        }

        return float.TryParse(token, out value);
    }

    public static bool TryParsePosition(string[] tokens, int start, Vec3f origin, out Vec3f position)
    {
        position = new Vec3f();
        if (start + 2 >= tokens.Length)
        {
            return false;
        }

        if (!TryParsePositionComponent(tokens[start], origin.X, out float x) ||
            !TryParsePositionComponent(tokens[start + 1], origin.Y, out float y) ||
            !TryParsePositionComponent(tokens[start + 2], origin.Z, out float z))
        {
            return false;
        }

        position = new Vec3f { X = x, Y = y, Z = z };
        return true;
    }

    public static EntityInstance[] ResolveTargets(ServerInstance server, Player? player, string token)
    {
        if (token == "@s")
        {
            return player is null ? [] : [player];
        }

        if (token == "@a")
        {
            return server.Players.Values.ToArray<EntityInstance>();
        }

        if (token == "@e")
        {
            if (player is not null)
            {
                return player.Dimension?.Entities.ToArray() ?? [];
            }

            return server.Worlds.SelectMany(world => world.Dimensions).SelectMany(dimension => dimension.Entities).ToArray();
        }

        if (token == "@p")
        {
            Player? nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (Player candidate in server.Players.Values)
            {
                if (player is not null && candidate.Dimension != player.Dimension)
                {
                    continue;
                }

                float dx = candidate.Position.X - (player?.Position.X ?? candidate.Position.X);
                float dy = candidate.Position.Y - (player?.Position.Y ?? candidate.Position.Y);
                float dz = candidate.Position.Z - (player?.Position.Z ?? candidate.Position.Z);
                float distance = dx * dx + dy * dy + dz * dz;
                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistance = distance;
            }

            return nearest is null ? [] : [nearest];
        }

        foreach (Player candidate in server.Players.Values)
        {
            if (string.Equals(candidate.Username, token, StringComparison.OrdinalIgnoreCase))
            {
                return [candidate];
            }
        }

        return [];
    }

    public static string[] ResolveOfflineTargets(ServerInstance server, string token, EntityInstance[] onlineTargets)
    {
        if (onlineTargets.Length > 0 || token.StartsWith('@'))
        {
            return [];
        }

        return [];
    }

    public static List<Player> ResolvePlayers(ServerInstance server, Player? context, string token)
    {
        List<Player> players = [];
        EntityInstance[] entities = ResolveTargets(server, context, token);
        for (int i = 0; i < entities.Length; i++)
        {
            if (entities[i] is Player player)
            {
                players.Add(player);
            }
        }

        return players;
    }

    public static string[] GetRegisteredDimensionIdentifiers(WorldInstance world)
    {
        List<string> identifiers = [];
        foreach (DimensionInstance dimension in world.Dimensions)
        {
            identifiers.Add(dimension.Identifier);
        }

        identifiers.Sort(StringComparer.OrdinalIgnoreCase);
        return identifiers.ToArray();
    }

    public static bool TryFindRegisteredDimension(WorldInstance world, string identifier, out DimensionInstance? dimension)
    {
        dimension = world.GetDimension(identifier);
        return dimension is not null;
    }

    public static bool IsRegisteredDimensionToken(WorldInstance world, string token) =>
        TryFindRegisteredDimension(world, token, out _);

    public static bool TryStripTrailingDimension(
        WorldInstance world,
        string[] args,
        out string[] stripped,
        out string? dimensionIdentifier)
    {
        stripped = args;
        dimensionIdentifier = null;

        if (args.Length == 0)
        {
            return false;
        }

        string last = args[^1];
        if (!IsRegisteredDimensionToken(world, last))
        {
            return false;
        }

        dimensionIdentifier = last;
        stripped = args[..^1];
        return true;
    }

    public static CommandResult? ResolveSinglePlayerTarget(
        ServerInstance server,
        Player? context,
        string token,
        string emptyMessage,
        string ambiguousMessage)
    {
        if (token == "@a")
        {
            return CommandResult.Message(ambiguousMessage, false);
        }

        List<Player> players = ResolvePlayers(server, context, token);
        if (players.Count == 0)
        {
            return CommandResult.Message(emptyMessage, false);
        }

        if (players.Count > 1)
        {
            return CommandResult.Message(ambiguousMessage, false);
        }

        return null;
    }
}

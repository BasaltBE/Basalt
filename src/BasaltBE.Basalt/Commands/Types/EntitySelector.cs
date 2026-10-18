using System.Globalization;
using System.Numerics;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Dimensions;

namespace BasaltBE.Basalt.Commands.Types;

public sealed class EntitySelector {
    private static readonly EntitySelector Self = new() { kind = 's' };
    private static readonly EntitySelector AllPlayers = new() { kind = 'a' };
    private static readonly EntitySelector AllEntities = new() { kind = 'e' };
    private static readonly EntitySelector NearestPlayer = new() { kind = 'p' };
    private static readonly EntitySelector RandomPlayer = new() { kind = 'r' };
    private char kind;
    private string? name;
    private List<(string Key, string Value, bool Negated)>? filters;
    private Vector3 origin;
    private Vector3 extent;
    private byte originAxes;
    private byte extentAxes;
    private float minimumRadius;
    private float maximumRadius = float.MaxValue;
    private int count;

    public static bool Parse(ReadOnlySpan<char> text, out EntitySelector? selector) {
        selector = null;
        if (text.IsEmpty)
            return false;
        if (text.Length == 2 && text[0] == '@') {
            selector = text[1] switch {
                's' => Self,
                'a' => AllPlayers,
                'e' => AllEntities,
                'p' => NearestPlayer,
                'r' => RandomPlayer,
                _ => null
            };
            return selector is not null;
        }
        EntitySelector parsed = new();
        if (text[0] != '@') {
            parsed.name = CommandReader.Text(text);
            selector = parsed;
            return parsed.name.Length > 0;
        }
        if (text.Length < 2 || text[1] is not ('s' or 'a' or 'e' or 'p' or 'r'))
            return false;
        parsed.kind = text[1];
        if (text.Length < 4 || text[2] != '[' || text[^1] != ']')
            return false;
        ReadOnlySpan<char> remaining = text[3..^1].Trim();
        HashSet<string> keys = new(StringComparer.Ordinal);
        while (!remaining.IsEmpty) {
            int end = 0;
            bool quoted = false;
            bool escaped = false;
            for (; end < remaining.Length; end++) {
                char character = remaining[end];
                if (escaped) escaped = false;
                else if (quoted && character == '\\') escaped = true;
                else if (character == '"') quoted = !quoted;
                else if (!quoted && character == ',') break;
            }
            if (quoted)
                return false;
            ReadOnlySpan<char> filter = remaining[..end].Trim();
            int equals = filter.IndexOf('=');
            if (equals <= 0 || equals == filter.Length - 1)
                return false;
            string key = filter[..equals].Trim().ToString();
            ReadOnlySpan<char> rawValue = filter[(equals + 1)..].Trim();
            bool negated = rawValue[0] == '!';
            if (negated) rawValue = rawValue[1..].Trim();
            if (rawValue.IsEmpty)
                return false;

            if (key is "name" or "type" or "family") {
                string value = CommandReader.Text(rawValue);
                if (value.Length == 0)
                    return false;
                if (key == "type" && !value.Contains(':')) value = "minecraft:" + value;
                (parsed.filters ??= []).Add((key, value, negated));
            }
            else {
                if (negated || !keys.Add(key))
                    return false;
                if (key == "c") {
                    if (!int.TryParse(rawValue, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out parsed.count) ||
                        parsed.count == 0 || parsed.count < -1024 || parsed.count > 1024)
                        return false;
                }
                else {
                    if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) ||
                        !float.IsFinite(number) || MathF.Abs(number) > 30_000_000)
                        return false;
                    switch (key) {
                        case "x": parsed.origin.X = number; parsed.originAxes |= 1; break;
                        case "y": parsed.origin.Y = number; parsed.originAxes |= 2; break;
                        case "z": parsed.origin.Z = number; parsed.originAxes |= 4; break;
                        case "dx": parsed.extent.X = number; parsed.extentAxes |= 1; break;
                        case "dy": parsed.extent.Y = number; parsed.extentAxes |= 2; break;
                        case "dz": parsed.extent.Z = number; parsed.extentAxes |= 4; break;
                        case "r" when number >= 0: parsed.maximumRadius = number; break;
                        case "rm" when number >= 0: parsed.minimumRadius = number; break;
                        default: return false;
                    }
                }
            }
            if (end == remaining.Length)
                break;
            remaining = remaining[(end + 1)..].Trim();
            if (remaining.IsEmpty)
                return false;
        }
        if (parsed.minimumRadius > parsed.maximumRadius)
            return false;
        selector = parsed;
        return true;
    }

    public Entity[] Resolve(CommandContext context, bool playersOnly = false) {
        if (name is not null) {
            Player? named = context.Server.GetPlayer(name);
            return named is { Spawned: true, Disposed: false } ? [named] : [];
        }
        Player? sender = context.Player;
        bool spatial = originAxes != 0 || extentAxes != 0 || minimumRadius != 0 || maximumRadius != float.MaxValue ||
            kind == 'p' || count != 0;
        if (spatial && sender is null && originAxes != 7)
            return [];
        Vector3 center = new(sender?.Position.X ?? 0, sender?.Position.Y ?? 0, sender?.Position.Z ?? 0);
        if ((originAxes & 1) != 0) center.X = origin.X;
        if ((originAxes & 2) != 0) center.Y = origin.Y;
        if ((originAxes & 4) != 0) center.Z = origin.Z;
        if (kind == 's')
            return sender is { Spawned: true, Disposed: false } && Matches(sender, center) ? [sender] : [];

        bool single = kind is 'p' or 'r' && (count == 0 || Math.Abs(count) == 1);
        List<Entity>? found = single ? null : [];
        Entity? selected = null;
        float selectedDistance = count < 0 ? float.MinValue : float.MaxValue;
        int matches = 0;
        foreach (World world in context.Server.Worlds) {
            foreach (Dimension dimension in world.Dimensions.Values) {
                if (sender is not null && (spatial || kind == 'e') && !ReferenceEquals(dimension, sender.Dimension))
                    continue;
                foreach (Entity entity in dimension.GetEntities(playersOnly || kind != 'e')) {
                    if (!Matches(entity, center))
                        continue;
                    if (!single) {
                        found!.Add(entity);
                        continue;
                    }
                    if (kind == 'r') {
                        if (Random.Shared.Next(++matches) == 0)
                            selected = entity;
                    }
                    else {
                        float distance = Distance(entity, center);
                        if (selected is null || (count < 0 ? distance > selectedDistance : distance < selectedDistance)) {
                            selected = entity;
                            selectedDistance = distance;
                        }
                    }
                }
            }
        }
        if (single)
            return selected is null ? [] : [selected];
        if (found!.Count == 0)
            return [];
        if (kind == 'r')
            Random.Shared.Shuffle(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(found));
        else if (kind == 'p' || count != 0) {
            found.Sort(new SelectorDistanceComparer(center, count < 0));
        }
        int limit = count == 0 ? (kind is 'p' or 'r' ? 1 : found.Count) : Math.Abs(count);
        if (found.Count > limit)
            found.RemoveRange(limit, found.Count - limit);
        return found.ToArray();
    }

    private bool Matches(Entity entity, Vector3 center) {
        if (entity is Player { Spawned: false } or Player { Disposed: true })
            return false;
        if (filters is not null) {
            foreach ((string key, string value, bool negated) in filters) {
                bool matches = key switch {
                    "type" => entity.Type?.Identifier == value,
                    "name" => entity is Player player && string.Equals(player.Username, value, StringComparison.OrdinalIgnoreCase),
                    "family" => entity.Type is { } type && Array.IndexOf(type.Families, value) >= 0,
                    _ => false
                };
                if (matches == negated)
                    return false;
            }
        }
        if (minimumRadius != 0 || maximumRadius != float.MaxValue) {
            float distance = Distance(entity, center);
            if (distance < minimumRadius * minimumRadius || distance > (double)maximumRadius * maximumRadius)
                return false;
        }
        Vector3 delta = new(entity.Position.X - center.X, entity.Position.Y - center.Y, entity.Position.Z - center.Z);
        if (extentAxes != 0 && (delta.X < MathF.Min(0, extent.X) || delta.X >= MathF.Max(0, extent.X) + 1 ||
            delta.Y < MathF.Min(0, extent.Y) || delta.Y >= MathF.Max(0, extent.Y) + 1 ||
            delta.Z < MathF.Min(0, extent.Z) || delta.Z >= MathF.Max(0, extent.Z) + 1))
            return false;
        return true;
    }

    private static float Distance(Entity entity, Vector3 center) =>
        Vector3.DistanceSquared(new Vector3(entity.Position.X, entity.Position.Y, entity.Position.Z), center);
}

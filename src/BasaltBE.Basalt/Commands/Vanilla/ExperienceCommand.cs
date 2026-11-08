using System.Text;
using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class ExperienceCommand : Command {
    public ExperienceCommand() : base(
        "experience",
        "Adds or removes player experience, and can set or query experience.",
        "basalt.command.xp",
        ["xp"]
    ) {
        CustomEnum actions = new("ExperienceAction", ["add", "set", "query"]);
        CustomEnum units = new("ExperienceUnit", ["points", "levels"]);
        AddOverload(
            new("action", actions),
            new("target", new TargetEnum(playersOnly: true)),
            new("amount", new IntEnum()),
            new("unit", units, optional: true)
        );
        AddOverload(
            new("action", actions),
            new("target", new TargetEnum(playersOnly: true)),
            new("unit", units)
        );
    }

    public override CommandResult Execute(CommandContext context) {
        string action = context.Get<string>("action").ToLowerInvariant();
        Entity[] targets = context.Get<Entity[]>("target");
        bool hasAmount = Array.Exists(context.Parameters, static parameter => parameter.Name == "amount");
        int amount = context.Get("amount", 0);
        string unit = context.Get("unit", action == "query" ? string.Empty : "points").ToLowerInvariant();

        if (targets.Length == 0) return new(false, "No players matched the target.");
        if (targets.Length > 128) return new(false, "Select no more than 128 players.");

        if (action is not ("add" or "set" or "query")) return new(false, "Use add, set, or query.");

        if (unit is not ("points" or "levels")) return new(false, "Use points or levels.");

        if ((action == "query") == hasAmount) return new(false, "Query requires a unit. Add and set require an amount.");

        if (action == "set" && amount < 0) return new(false, "Experience cannot be set below zero.");

        if (action == "set" && unit == "levels" && amount > PlayerExperienceTrait.MaximumLevel)
            return new(false, $"Player levels cannot exceed {PlayerExperienceTrait.MaximumLevel}.");

        List<string> changed = [];
        StringBuilder query = new();
        foreach (Entity entity in targets) {
            if (entity is not Player { Spawned: true, Disposed: false } player ||
                player.GetTrait<PlayerExperienceTrait>() is not { } experience)
                continue;

            using Lock.Scope tickSync = player.TickSync.EnterScope();
            using Lock.Scope sync = player.Sync.EnterScope();

            if (action == "query") {
                if (query.Length > 0) query.Append('\n');
                query.Append(FormattableString.Invariant(
                    $"{player.Username} has {(unit == "levels" ? experience.Level : experience.Points)} {unit}."
                ));

            changed.Add(player.Username);
                continue;
            }

            if (unit == "levels") {
                if (action == "add" && (long)experience.Level + amount > PlayerExperienceTrait.MaximumLevel) continue;
                if (action == "add") experience.AddLevels(amount);
                else experience.SetLevels(amount);
            } else if (action == "add")
                experience.AddPoints(amount);
            else if (!experience.SetPoints(amount)) continue;

            changed.Add(player.Username);
        }

        if (changed.Count == 0)
            return new(false, action == "set" && unit == "points"
                ? "The points value must be less than the points needed for the next level."
                : "No selected players are online and spawned.");

        if (action == "query") return new(true, query.ToString());

        string names = string.Join(", ", changed);
        string result = action switch {
            "set" when unit == "levels" => FormattableString.Invariant($"Set {amount} levels for {names}."),
            "set" => FormattableString.Invariant($"Set experience to {amount} points for {names}."),
            "add" when unit == "levels" && amount < 0 => FormattableString.Invariant($"Taken {-(long)amount} levels from {names}."),
            "add" when unit == "levels" => FormattableString.Invariant($"Gave {amount} levels to {names}."),
            "add" when amount < 0 => FormattableString.Invariant($"Removed {-(long)amount} experience from {names}."),
            _ => FormattableString.Invariant($"Gave {amount} experience to {names}.")
        };
        return new(true, result);
    }
}

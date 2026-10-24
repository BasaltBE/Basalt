using System.Globalization;
using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class ExperienceCommand : Command {
    public ExperienceCommand() : base(
        "experience",
        "Add, set, or query player experience.",
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

        if (targets.Length is 0 or > 128)
            return new(false, "Select between 1 and 128 players.");

        if (action is not ("add" or "set" or "query"))
            return new(false, "Use add, set, or query.");

        if (unit is not ("points" or "levels"))
            return new(false, "Use points or levels.");

        if ((action == "query") == hasAmount)
            return new(false, "Query needs a unit. Add and set need an amount.");

        if (action == "set" && amount < 0)
            return new(false, "Experience cannot be set below zero.");

        if (action == "set" && unit == "levels" && amount > PlayerExperienceTrait.MaximumLevel)
            return new(false, $"Player levels cannot exceed {PlayerExperienceTrait.MaximumLevel}.");

        int changed = 0;
        int lastValue = 0;
        foreach (Entity entity in targets) {
            if (entity is not Player { Spawned: true, Disposed: false } player ||
                player.GetTrait<PlayerExperienceTrait>() is not { } experience)
                continue;
            using Lock.Scope tickSync = player.TickSync.EnterScope();
            using Lock.Scope sync = player.Sync.EnterScope();
            if (action == "query") {
                lastValue = unit == "levels" ? experience.Level : experience.Points;
                changed++;
                continue;
            }

            if (unit == "levels") {
                if (action == "add" && (long)experience.Level + amount > PlayerExperienceTrait.MaximumLevel)
                    continue;
                if (action == "add")
                    experience.AddLevels(amount);
                else
                    experience.SetLevels(amount);
            }
            else if (action == "add")
                experience.AddPoints(amount);
            else if (!experience.SetPoints(amount))
                continue;
            changed++;
            lastValue = unit == "levels" ? experience.Level : experience.Points;
        }

        if (changed == 0)
            return new(false, action == "set" && unit == "points"
                ? "Points must be less than the points needed for the next level."
                : "No selected players are online and spawned.");

        string result = action == "query"
            ? FormattableString.Invariant($"{changed} player(s) have {lastValue} {unit}.")
            : FormattableString.Invariant($"{(action == "add" ? "Added" : "Set")} {amount} {unit} for {changed} player(s).");
        return new(true, result);
    }
}

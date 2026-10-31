using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Types;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class GameRuleCommand : Command {
    public GameRuleCommand() : base("gamerule", "Sets or queries a game rule value.", "basalt.command.gamerule") {
        CustomEnum rules = new("GameRule", ["keepinventory", "showcoordinates", "falldamage", "tntexplodes"]);
        AddOverload(new CommandParameter("rule", rules));
        AddOverload(new CommandParameter("rule", rules), new CommandParameter("value", new BoolEnum()));
    }

    public override CommandResult Execute(CommandContext context) {
        string name = context.Get<string>("rule");
        if (!Enum.TryParse(name, ignoreCase: true, out GameRule rule) || !Enum.IsDefined(rule))
            return new(false, $"Unknown game rule '{name}'.");

        World world = context.Player?.Dimension.World ?? context.Server.GetWorld();

        if (Array.Exists(context.Parameters, static parameter => parameter.Name == "value")) {
            bool value = context.Get<bool>("value");
            bool changed = world.GameRules.Set(rule, value);
            string state = value ? "true" : "false";
            return new(true, changed
                ? $"Game rule {name} has been updated to {state}."
                : $"Game rule {name} is already {state}.");
        }

        return new(true, $"Game rule {name} is {(world.GameRules.Get(rule) ? "true" : "false")}.");
    }
}

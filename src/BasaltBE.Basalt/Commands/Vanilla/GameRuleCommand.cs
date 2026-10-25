using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Types;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class GameRuleCommand : Command {
    public GameRuleCommand() : base("gamerule", "View or change a world game rule.", "basalt.command.gamerule") {
        CustomEnum rules = new("GameRule", ["keepinventory", "showcoordinates", "falldamage"]);
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
            return new(true, changed
                ? $"{name} is {(value ? "on" : "off")}."
                : $"{name} is already {(value ? "on" : "off")}.");
        }

        return new(true, $"{name} is {(world.GameRules.Get(rule) ? "on" : "off")}.");
    }
}

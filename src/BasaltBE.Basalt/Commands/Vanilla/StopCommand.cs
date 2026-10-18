using BasaltBE.Basalt.Commands.Types;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class StopCommand : Command {
    public StopCommand() : base("stop", "Stop the server.", "basalt.operator") {
        AddOverload();
    }

    public override CommandResult Execute(CommandContext context) {
        context.Server.Stop();
        return new(true, "Stopping the server.");
    }
}

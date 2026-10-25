using BasaltBE.Basalt.Commands.Types;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class SaveAllCommand : Command {
    public SaveAllCommand() : base("save-all", "Save all worlds and online player data.", "basalt.operator") {
        AddOverload();
    }

    public override CommandResult Execute(CommandContext context) {
        bool queued = context.Server.SaveAll();
        return new(queued, queued
            ? "Save queued for all worlds and online players."
            : "Some saves could not be queued because a storage queue is full.");
    }
}

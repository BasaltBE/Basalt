using BasaltBE.Basalt.Commands.Types;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class SaveAllCommand : Command {
    public SaveAllCommand() : base("save-all", "Saves world and player data.", "basalt.operator") {
        AddOverload();
    }

    public override CommandResult Execute(CommandContext context) {
        bool queued = context.Server.SaveAll();
        return new(queued, queued
            ? "Saving all world and player data."
            : "Could not queue all saves because a storage queue is full.");
    }
}

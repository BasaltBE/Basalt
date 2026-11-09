namespace BasaltBE.Basalt.Commands;

public sealed class ServerCommandExecutor(Server server) : CommandExecutor(server, null) {
    public override bool Allows(string permission) => true;
}

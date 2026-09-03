namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Commands;
using Basalt.Core.Events;
using Basalt.Core.Profiling;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public static class CommandRequest {
    public static void Handle(Server server, NetworkConnection connection, CommandRequestPacket packet) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("CommandRequest.Handle") : default;

        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            QueueResponse(server, connection, CommandResult.Error("Command executor was not found."), packet.Origin);
            return;
        }

        if (player.Dimension is not { } dimension) {
            QueueResponse(server, connection, CommandResult.Error("Command executor has no dimension."), packet.Origin);
            return;
        }

        string command = packet.Command;
        CommandOriginData origin = new() {
            Type = packet.Origin.Type,
            Uuid = new Uuid {
                MostSignificantBits = packet.Origin.Uuid.MostSignificantBits,
                LeastSignificantBits = packet.Origin.Uuid.LeastSignificantBits
            },
            RequestId = packet.Origin.RequestId,
            PlayerId = packet.Origin.PlayerId
        };

        dimension.TryEnqueue(player, () => Process(server, connection, player, command, origin));
    }

    private static void Process(
        Server server,
        NetworkConnection connection,
        Player.Player player,
        string command,
        CommandOriginData origin) {
        CommandResult result;
        try {
            Logger.Info($"{player.Username} executed command {command}");

            string commandName = command.Split(' ', 2)[0].TrimStart('/');
            CommandDefinition? definition = server.Commands.FindCommand(commandName);

            PlayerCommandSignal signal = new(player, command, definition);
            server.Emit(signal);

            result = signal.Cancelled
                ? CommandResult.Fail
                : server.Commands.Execute(server, player, command);
        }
        catch (Exception exception) {
            result = CommandResult.Error(exception.Message);
            Logger.Warn($"Command request failed: {exception}");
        }

        QueueResponse(server, connection, result, origin);
    }

    private static void QueueResponse(
        Server server,
        NetworkConnection connection,
        CommandResult result,
        CommandOriginData origin) {

        List<CommandOutputMessage> messages = [];
        if (result.Message is not null) {
            messages.Add(new CommandOutputMessage {
                MessageId = result.Message,
                Parameters = [],
                Successful = result.Success,
            });
        }

        CommandOutputPacket response = new() {
            Output = new CommandOutput() {
                Messages = messages.ToArray(),
                DataSet = string.Empty,
                OutputType = CommandOutputType.AllOutput,
                SuccessCount = result.Success ? 1U : 0U,
            },
            OriginData = origin,
        };

        server.Network.QueuePacket(connection, response);
    }
}

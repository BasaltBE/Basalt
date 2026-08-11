namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Commands;
using Basalt.Core.Events;
using Basalt.Core.Profiling;
using Basalt.RakNet;
using BedrockProtocol.Enums;
using BedrockProtocol.Packets;
using BedrockProtocol.Types;

public static class CommandRequest {
    public static void Handle(Server server, NetworkConnection connection, CommandRequestPacket packet) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("CommandRequest.Handle") : default;

        CommandResult result = CommandResult.Fail;

        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            result = CommandResult.Error("Command executor was not found.");
        }
        else {
            try {
                Logger.Info($"{player.Username} executed command {packet.Command}");

                // Emit PlayerCommand signal
                string commandName = packet.Command.Split(' ', 2)[0].TrimStart('/');
                CommandDefinition? definition = server.Commands.FindCommand(commandName);

                PlayerCommandSignal signal = new(player, packet.Command, definition);
                server.Emit(signal);

                if (signal.Cancelled) {
                    result = CommandResult.Fail;
                }
                else {
                    result = server.Commands.Execute(server, player, packet.Command);
                }
            }
            catch (Exception exception) {
                result = CommandResult.Error(exception.Message);
                Logger.Warn($"Command request failed: {exception}");
            }
        }

        List<CommandOutputMessage> messages = [];
        if (result.Message is not null) {
            messages.Add(new CommandOutputMessage {
                MessageID = result.Message,
                Parameters = [],
                Successful = result.Success,
            });
        }

        CommandOutputPacket response = new() {
            Output = new BedrockProtocol.Types.CommandOutput() {
                OutputMessages = messages,
                DataSet = string.Empty,
                OutputType = CommandOutputType.AllOutput,
                SuccessCount = result.Success ? 1U : 0U,
            },
            OriginData = packet.Origin,
        };

        server.Network.QueuePacket(connection, response);
    }
}

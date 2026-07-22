namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Commands;
using Basalt.Core.Events;
using Basalt.Core.Profiling;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;

public static class CommandRequest {
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer) {
        using var __zone = Profiler.BeginZone("CommandRequest.Handle");
        CommandRequestPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet = (CommandRequestPacket)Protocol.Io.Packet.Deserialize(reader);

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
                Message = result.Message,
                Parameters = [],
                Success = result.Success
            });
        }

        CommandResponsePacket response = new() {
            SuccessCount = result.Success ? 1U : 0U,
            OutputType = CommandOutputType.AllOutput,
            DataSet = string.Empty,
            Origin = packet.Origin,
            OutputMessages = messages
        };

        server.Network.SendPacket(connection, response);
    }
}

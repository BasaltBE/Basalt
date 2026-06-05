namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Commands;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;


public static class CommandRequest
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        CommandRequestPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet = (CommandRequestPacket)Protocol.Io.Packet.Deserialize(reader);


        CommandResult result = CommandResult.Empty(false);

        if (!server.Players.TryGetValue(connection, out global::Basalt.Core.Player.Player? player))
        {
            result = CommandResult.Message("Command executor was not found.", false);
        }
        else
        {
            try
            {
                Logger.Info($"{player.Username} executed command {packet.Command}");

                string[] tokens = packet.Command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (tokens.Length == 0)
                {
                    result = CommandResult.Empty(false);
                }
                else
                {
                    string commandName = tokens[0].TrimStart('/');
                    Basalt.Core.Commands.Command command = server.Commands.Get(commandName);
                    if (!CommandRegistry.CanPlayerExecute(command, player))
                    {
                        result = CommandResult.Message(CommandRegistry.PermissionDeniedMessage, false);
                    }
                    else
                    {
                        result = server.Commands.Execute(server, player, packet.Command);
                    }
                }
            }
            catch (KeyNotFoundException)
            {
                result = server.Commands.Execute(server, player, packet.Command);
            }
            catch (Exception exception)
            {
                result = CommandResult.Message(exception.Message, false);
                Logger.Warn($"Command request failed: {exception}");
            }
        }

        CommandResponsePacket response = new()
        {
            SuccessCount = result.Success ? 1U : 0U,
            OutputType = CommandOutputType.AllOutput,
            DataSet = string.Empty,
            Origin = packet.Origin,
            OutputMessages = result.Messages.Select(message => new CommandOutputMessage
            {
                Message = message,
                Parameters = [],
                Success = result.Success
            }).ToList()
        };

        server.Network.SendPacket(connection, response);
    }
}











namespace Basalt.Server.Network.Handlers;

using Basalt.Server;
using Basalt.Server.Commands;
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

        Logger.Info($"Received command request: {packet.Command}");

        CommandResult result = CommandResult.Empty(false);

        if (!server.Players.TryGetValue(connection, out global::Basalt.Server.Player.Player? player))
        {
            result = CommandResult.Message("Command executor was not found.", false);
        }
        else
        {
            try
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











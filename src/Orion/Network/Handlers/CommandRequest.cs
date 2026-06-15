namespace Orion.Network.Handlers;

using Orion;
using Orion.Commands;
using Orion.Player;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;
using Orion.RakNet;


public static class CommandRequest
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        CommandRequestPacket packet = new();
        int offset = 0;
        BinaryReader reader = new(packetBuffer, ref offset);
        packet = (CommandRequestPacket)Protocol.Io.Packet.Deserialize(reader);


        CommandResult result = CommandResult.Empty(false);

        if (!SessionLookup.TryGetPlayer(server, connection, out global::Orion.Player.Player? player))
        {
            result = CommandResult.Message("Command executor was not found.", false);
        }
        else
        {
            try
            {
                Info($"{player.Username} executed command {packet.Command}");

                string[] tokens = packet.Command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (tokens.Length == 0)
                {
                    result = CommandResult.Empty(false);
                }
                else
                {
                    string commandName = tokens[0].TrimStart('/');
                    Orion.Commands.Command command = server.Commands.Get(commandName);
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
                Warn($"Command request failed: {exception}");
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

        if (SessionLookup.TryGetSession(server, connection, out PlayerSession? session))
        {
            session.Send(response);
        }
        else
        {
            server.Network.SendPacket(connection, response);
        }
    }
}











using Orion.Commands;
using Orion.PluginContracts.Registry;
using Player = global::Orion.Player.Player;

namespace Orion.Plugins.Registry;

internal sealed class CommandRegistryFacade(ContentRegistriesCore core) : ICommandRegistry
{
    Orion.Commands.CommandRegistry? _backend;

    public void Bind(Orion.Commands.CommandRegistry backend) => _backend = backend;

    public void ResetForTests() => _backend = null;

    public void Register(IPluginCommand command) =>
        throw new InvalidOperationException("Use plugin-scoped registries from IPluginContext.Registries.");

    internal void Register(string pluginId, IPluginCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name);
        core.ThrowIfCommandsFrozen();

        if (_backend is null)
        {
            throw new InvalidOperationException(
                "Commands can only be registered after PluginHost.EnableAll(Server) binds the command registry.");
        }

        if (!core.TryClaimCommand(pluginId, command.Name))
        {
            return;
        }

        _backend.Register(new PluginCommandAdapter(command));
    }

    sealed class PluginCommandAdapter : Command
    {
        readonly IPluginCommand _plugin;

        public PluginCommandAdapter(IPluginCommand plugin)
            : base(
                plugin.Name.ToLowerInvariant(),
                plugin.Description,
                plugin.Aliases.Select(a => a.ToLowerInvariant()).ToArray(),
                [])
        {
            _plugin = plugin;
        }

        public override CommandResult? ExecuteManual(CommandExecutionState state, string[] tokens, int argumentOffset)
        {
            string senderName = state.Executor switch
            {
                PlayerExecutor playerExecutor => playerExecutor.Player.Username,
                _ => "Server"
            };

            string[] args = tokens.Length > argumentOffset
                ? tokens[argumentOffset..]
                : [];

            PluginCommandContext context = new(
                senderName,
                args,
                message =>
                {
                    switch (state.Executor)
                    {
                        case PlayerExecutor playerExecutor:
                            playerExecutor.SendMessage(message);
                            break;
                        case ServerExecutor serverExecutor:
                            serverExecutor.SendMessage(message);
                            break;
                    }
                });

            _plugin.Execute(context);
            return CommandResult.Empty(true);
        }
    }

    sealed class PluginCommandContext(
        string senderName,
        IReadOnlyList<string> arguments,
        Action<string> reply) : IPluginCommandContext
    {
        public string SenderName { get; } = senderName;
        public IReadOnlyList<string> Arguments { get; } = arguments;
        public void Reply(string message) => reply(message);
    }
}

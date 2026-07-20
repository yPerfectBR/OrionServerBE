namespace Orion.Commands.List.Operator;

using Orion.Commands;
using Orion.PluginContracts.Diagnostics;
using Orion.Plugins;
using Orion.Protocol.Registry;

public class PluginsCommand : Command
{
    public PluginsCommand() : base("plugins", "List loaded plugins")
    {
        Permissions.Add("basalt.op");
    }

    public override CommandResult Execute(CommandExecutionState state)
    {
        _ = state;
        IReadOnlyList<string> loaded = PluginHost.LoadedPluginIds;
        if (loaded.Count == 0)
        {
            IReadOnlyList<string> creative = CuratedItemCatalog.GetLoadedCreativePlugins();
            if (creative.Count == 0)
            {
                return CommandResult.Message(
                    "§r§7Plugins (§a0§7)\n§7` No plugins loaded. Enable Plugins.Enabled (McMaster) in server.json.",
                    true);
            }

            string creativeList = string.Join("§7, §a", creative);
            return CommandResult.Message(
                $"§r§7Plugins (§a{creative.Count}§7)\n§7` §a{creativeList}§7 (creative tab registrations)",
                true);
        }

        string list = string.Join(
            "\n§7` ",
            PluginHost.LoadedManifests.Select(FormatManifestLine));

        IReadOnlyList<string> serviceTypes = PluginHost.Services.ListServiceTypeNames();
        string servicesLine = serviceTypes.Count == 0
            ? ""
            : $"\n§7Services: §a{string.Join("§7, §a", serviceTypes)}";

        IReadOnlyList<PluginConflict> conflicts = PluginHost.Diagnostics.Conflicts;
        string conflictsLine = conflicts.Count == 0
            ? ""
            : $"\n§7Conflicts (§e{conflicts.Count}§7)\n§7` {string.Join("\n§7` ", conflicts.Select(FormatConflictLine))}";

        return CommandResult.Message(
            $"§r§7Plugins (§a{loaded.Count}§7)\n§7` {list}{servicesLine}{conflictsLine}",
            true);
    }

    static string FormatManifestLine(PluginContracts.IPluginManifest manifest)
    {
        string line = $"§a{manifest.Id}§7 v{manifest.Version}";
        if (manifest.Provides.Count > 0)
        {
            line += $" §8[provides: {string.Join(", ", manifest.Provides)}]";
        }

        if (manifest.SoftDepend.Count > 0)
        {
            line += $" §8(softdepend: {string.Join(", ", manifest.SoftDepend.Select(d => d.Id))})";
        }

        return line;
    }

    static string FormatConflictLine(PluginConflict conflict) =>
        $"§eWARN §7{conflict.Kind} §a{conflict.Key} §7{conflict.WinnerPluginId} §c> §7{conflict.LoserPluginId}";
}

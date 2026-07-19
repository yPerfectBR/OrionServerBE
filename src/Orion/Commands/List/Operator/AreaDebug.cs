namespace Orion.Commands.List.Operator;

using Orion.Commands;
using Orion.Player;
using Orion.Scheduling;
using Orion.World;
using Orion.World.Threading;

public sealed class AreaDebugCommand : Command
{
    public AreaDebugCommand() : base("areadebug", "Show area scheduler worker metrics", ["regdebug", "regiondebug"], [])
    {
        Permissions.Add("basalt.op");
    }

    public override CommandResult Execute(CommandExecutionState state)
    {
        if (!state.Server.Properties.AreaThreadingEnabled || !state.Server.AreaScheduler.IsActive)
        {
            return CommandResult.Message("§7Area scheduler is disabled.", true);
        }

        IReadOnlyList<AreaWorkerLoadMetrics> metrics = state.Server.AreaScheduler.GetMetrics();
        if (metrics.Count == 0)
        {
            return CommandResult.Message("§7No area scheduler metrics available.", true);
        }

        System.Text.StringBuilder builder = new($"§7Area Scheduler ({metrics.Count} workers)\n");
        builder.Append(FormatRow(
            Cell("W", 2, "§7"),
            Cell("Areas", 5, "§7"),
            Cell("Pres", 5, "§7"),
            Cell("TPS", 5, "§7"),
            Cell("WorkMs", 6, "§7"),
            Cell("LagMs", 5, "§7")));
        builder.Append('\n');

        for (int i = 0; i < metrics.Count; i++)
        {
            AreaWorkerLoadMetrics metric = metrics[i];
            string tpsColor = metric.Tps < 10 ? "§c" : metric.Tps < 15 ? "§6" : "§a";
            builder.Append(FormatRow(
                Cell(metric.WorkerId.ToString(), 2, "§7"),
                Cell(metric.ActiveAreaCount.ToString(), 5, "§a"),
                Cell(metric.TotalPresenceCount.ToString(), 5, "§a"),
                Cell(metric.Tps.ToString("0.0"), 5, tpsColor),
                Cell(metric.LastTickWorkMs.ToString("0.0"), 6, "§a"),
                Cell(metric.TickLagMs.ToString("0.0"), 5, "§a")));
            builder.Append('\n');
        }

        builder.Append("§8--- §7Attached areas\n");
        World world = state.Server.GetWorld();
        foreach (Dimension dimension in world.Dimensions)
        {
            if (!dimension.UsesAreaThreading())
            {
                continue;
            }

            foreach (AreaShard shard in dimension.ShardManager.Shards)
            {
                if (!shard.IsAttached && shard.PresenceCount == 0)
                {
                    continue;
                }

                builder.Append(
                    $"§a{dimension.Identifier} §7#{shard.AreaIndex} §8{shard.Name} §8worker=§f{shard.AttachedWorkerId} §8presence=§f{shard.PresenceCount} §8chunks=§f{shard.ChunkCount}\n");
            }
        }

        if (state.Server.ConnectionCoordinator is ConnectionCoordinator coordinator && coordinator.IsActive)
        {
            builder.Append("§8--- §7Session workers\n");
            for (int workerId = 0; workerId < coordinator.Pool.WorkerCount; workerId++)
            {
                SessionWorker worker = coordinator.Pool.GetWorker(workerId);
                builder.Append(
                    $"§7sw{workerId} §8sessions=§f{worker.SessionCount} §8pending=§f{worker.PendingMessageCount}\n");
            }
        }

        builder.Append("§8--- §7Online players (§f").Append(state.Server.Sessions.Count).Append("§7)\n");
        foreach (PlayerSession session in state.Server.Sessions.Values)
        {
            if (session.ActiveEntity is not global::Orion.Player.Player player)
            {
                continue;
            }

            builder.Append($"§a{player.Username} §8pos=§f{player.Position.X:0.##} {player.Position.Y:0.##} {player.Position.Z:0.##}\n");
        }

        return CommandResult.Message(builder.ToString(), true);
    }

    static string Cell(string text, int width, string color)
        => color + text.PadLeft(width);

    static string FormatRow(params string[] cells)
    {
        System.Text.StringBuilder row = new();
        for (int i = 0; i < cells.Length; i++)
        {
            if (i > 0)
            {
                row.Append(" §8│ ");
            }

            row.Append(cells[i]);
        }

        return row.ToString();
    }
}

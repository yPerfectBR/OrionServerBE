namespace Orion.Scheduling;

public sealed class WorkerLoadMetrics
{
    public int WorkerId { get; init; }

    public int ActiveWorldCount { get; set; }

    public int TotalPresentPlayers { get; set; }

    public double LastTickWorkMs { get; set; }

    public double TickLagMs { get; set; }

    public double Tps { get; set; }
}

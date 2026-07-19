namespace Orion.Scheduling;

public sealed class AreaWorkerLoadMetrics
{
    public int WorkerId { get; set; }

    public int ActiveAreaCount { get; set; }

    public int TotalPresenceCount { get; set; }

    public double LastTickWorkMs { get; set; }

    public double TickLagMs { get; set; }

    public double Tps { get; set; }
}

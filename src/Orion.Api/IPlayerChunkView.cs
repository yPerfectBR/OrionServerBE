namespace Orion.Api;

/// <summary>Player chunk streaming / view-distance facade used by host call sites.</summary>
public interface IPlayerChunkView
{
    int ViewDistance { get; }

    string FormatDebugHudLine();
    void ApplyViewDistance(int distance);
    void StartChunkLoad();
    void ForceReloadViewDistance();
    void NotifyClientAtTeleportDestination();
    void AfterRegionHandoff();
    void InvalidateVisibleEntity(ulong runtimeId);
    void InvalidateVisibleEntityByUniqueId(long entityUniqueId);
}

namespace Orion.PluginContracts.Network;

/// <summary>Minimal connection facade for packet hooks (host adapts NetworkConnection).</summary>
public interface IPlayerConnection
{
    object? Native { get; }
}

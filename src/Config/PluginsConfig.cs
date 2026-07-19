using System.Text.Json.Serialization;

namespace Orion.Config;

/// <summary>
/// Optional C# plugin loading. Disabled by default — Orion does not auto-load plugins.
/// </summary>
public sealed class PluginsConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; init; }

    /// <summary>
    /// Directory relative to the process working directory (repo root when using <c>dotnet run</c>).
    /// </summary>
    [JsonPropertyName("Directory")]
    public string Directory { get; init; } = "plugins";

    /// <summary>
    /// <see cref="ConflictMode.Warn"/> (default): log and record conflicts.
    /// <see cref="ConflictMode.Fail"/>: throw after recording.
    /// </summary>
    [JsonPropertyName("ConflictMode")]
    [JsonConverter(typeof(ConflictModeJsonConverter))]
    public ConflictMode ConflictMode { get; init; } = ConflictMode.Warn;
}

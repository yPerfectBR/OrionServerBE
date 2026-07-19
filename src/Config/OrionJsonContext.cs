using System.Text.Json.Serialization;

namespace Orion.Config;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(OrionConfig))]
[JsonSerializable(typeof(ConflictMode))]
[JsonSerializable(typeof(WorldProperties))]
[JsonSerializable(typeof(WorldJsonFile))]
[JsonSerializable(typeof(ChunkPregenerationConfig))]
[JsonSerializable(typeof(List<ChunkPregenerationConfig>))]
[JsonSerializable(typeof(DimensionConfig))]
[JsonSerializable(typeof(ThreadingAreaConfig))]
public partial class OrionJsonContext : JsonSerializerContext;

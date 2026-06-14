using System.Text.Json.Serialization;

namespace Orion.Config;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(OrionConfig))]
[JsonSerializable(typeof(WorldProperties))]
public partial class OrionJsonContext : JsonSerializerContext;

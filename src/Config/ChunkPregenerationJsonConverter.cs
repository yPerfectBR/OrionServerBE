using System.Text.Json;
using System.Text.Json.Serialization;

namespace Orion.Config;

public sealed class ChunkPregenerationJsonConverter : JsonConverter<List<ChunkPregenerationConfig>>
{
    public override List<ChunkPregenerationConfig>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return [];
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            ChunkPregenerationConfig? single = JsonSerializer.Deserialize(
                ref reader,
                OrionJsonContext.Default.ChunkPregenerationConfig);
            return single is null ? [] : [single];
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            return JsonSerializer.Deserialize(ref reader, OrionJsonContext.Default.ListChunkPregenerationConfig) ?? [];
        }

        throw new JsonException("chunkPregeneration must be an object or array.");
    }

    public override void Write(Utf8JsonWriter writer, List<ChunkPregenerationConfig> value, JsonSerializerOptions options)
    {
        if (value.Count == 1)
        {
            JsonSerializer.Serialize(writer, value[0], OrionJsonContext.Default.ChunkPregenerationConfig);
            return;
        }

        JsonSerializer.Serialize(writer, value, OrionJsonContext.Default.ListChunkPregenerationConfig);
    }
}

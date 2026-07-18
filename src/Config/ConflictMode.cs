using System.Text.Json;
using System.Text.Json.Serialization;

namespace Orion.Config;

public enum ConflictMode
{
    Warn,
    Fail
}

public sealed class ConflictModeJsonConverter : JsonConverter<ConflictMode>
{
    public override ConflictMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        if (string.Equals(value, "fail", StringComparison.OrdinalIgnoreCase))
        {
            return ConflictMode.Fail;
        }

        return ConflictMode.Warn;
    }

    public override void Write(Utf8JsonWriter writer, ConflictMode value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value == ConflictMode.Fail ? "fail" : "warn");
    }
}

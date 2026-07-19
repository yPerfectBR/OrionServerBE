using System.Text.Json;
using System.Text.Json.Serialization;
using Orion.PluginContracts;

namespace Orion.Plugins;

public sealed class PluginManifest : IPluginManifest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public required string Id { get; init; }
    public required Version Version { get; init; }
    public required Version ApiVersion { get; init; }
    public required string Main { get; init; }
    public IReadOnlyList<string> Depend { get; init; } = [];
    public IReadOnlyList<string> SoftDepend { get; init; } = [];
    public IReadOnlyList<string> LoadBefore { get; init; } = [];
    public IReadOnlyList<string> Provides { get; init; } = [];
    public string DirectoryPath { get; init; } = "";
    public string AssemblyPath { get; init; } = "";

    public static PluginManifest ParseFile(string pluginJsonPath)
    {
        PluginManifestDto dto = JsonSerializer.Deserialize<PluginManifestDto>(
            File.ReadAllBytes(pluginJsonPath),
            JsonOptions)
            ?? throw new InvalidDataException($"Failed to parse {pluginJsonPath}");

        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            throw new InvalidDataException($"{pluginJsonPath}: missing id");
        }

        if (string.IsNullOrWhiteSpace(dto.Main))
        {
            throw new InvalidDataException($"{pluginJsonPath}: missing main");
        }

        if (!Version.TryParse(dto.Version, out Version? version))
        {
            throw new InvalidDataException($"{pluginJsonPath}: invalid version '{dto.Version}'");
        }

        if (!Version.TryParse(dto.Api, out Version? apiVersion))
        {
            throw new InvalidDataException($"{pluginJsonPath}: invalid api '{dto.Api}'");
        }

        string directory = Path.GetDirectoryName(pluginJsonPath)
            ?? throw new InvalidDataException($"Cannot resolve directory for {pluginJsonPath}");

        return new PluginManifest
        {
            Id = dto.Id,
            Version = version,
            ApiVersion = apiVersion,
            Main = dto.Main,
            Depend = dto.Depend ?? [],
            SoftDepend = dto.SoftDepend ?? [],
            LoadBefore = dto.LoadBefore ?? [],
            Provides = dto.Provides ?? [],
            DirectoryPath = directory,
            AssemblyPath = ResolveAssemblyPath(directory, dto.Id)
        };
    }

    static string ResolveAssemblyPath(string directory, string pluginId)
    {
        string expected = Path.Combine(directory, pluginId + ".dll");
        if (File.Exists(expected))
        {
            return expected;
        }

        string[] dlls = Directory.GetFiles(directory, "*.dll")
            .Where(p =>
            {
                string name = Path.GetFileName(p);
                return !name.StartsWith("Orion.", StringComparison.OrdinalIgnoreCase)
                    && !name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
                    && !name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
                    && !name.StartsWith("McMaster.", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        if (dlls.Length == 1)
        {
            return dlls[0];
        }

        throw new FileNotFoundException(
            $"Plugin assembly not found for '{pluginId}' (expected {pluginId}.dll in {directory})",
            expected);
    }

    private sealed class PluginManifestDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "0.0.0";

        [JsonPropertyName("api")]
        public string Api { get; set; } = "0.1.0";

        [JsonPropertyName("main")]
        public string Main { get; set; } = "";

        [JsonPropertyName("depend")]
        public List<string>? Depend { get; set; }

        [JsonPropertyName("softdepend")]
        public List<string>? SoftDepend { get; set; }

        [JsonPropertyName("loadbefore")]
        public List<string>? LoadBefore { get; set; }

        [JsonPropertyName("provides")]
        public List<string>? Provides { get; set; }
    }
}

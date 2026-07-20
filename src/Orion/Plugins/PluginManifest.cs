using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Orion.PluginContracts;

namespace Orion.Plugins;

public sealed partial class PluginManifest : IPluginManifest
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [GeneratedRegex(@"^[a-z_]{1,18}:[a-z_]{1,18}$", RegexOptions.CultureInvariant)]
    private static partial Regex PluginIdRegex();

    [GeneratedRegex(@"^[a-z0-9:-]{1,25}$", RegexOptions.CultureInvariant)]
    private static partial Regex FolderNameRegex();

    public required string Id { get; init; }
    public required Version Version { get; init; }
    public required Version ApiVersion { get; init; }
    public required string Main { get; init; }
    public IReadOnlyList<PluginDependency> Depend { get; init; } = [];
    public IReadOnlyList<PluginSoftDependency> SoftDepend { get; init; } = [];
    public IReadOnlyList<string> Provides { get; init; } = [];
    public string DirectoryPath { get; init; } = "";
    public string AssemblyPath { get; init; } = "";

    public static PluginManifest ParseFile(string pluginJsonPath)
    {
        PluginManifestDto dto = JsonSerializer.Deserialize<PluginManifestDto>(
            File.ReadAllBytes(pluginJsonPath),
            JsonOptions)
            ?? throw new InvalidDataException($"Failed to parse {pluginJsonPath}");

        string directory = Path.GetDirectoryName(pluginJsonPath)
            ?? throw new InvalidDataException($"Cannot resolve directory for {pluginJsonPath}");

        string folderName = Path.GetFileName(directory);
        ValidateId(dto.Id, pluginJsonPath);
        ValidateFolderName(folderName, dto.Id, pluginJsonPath);

        if (string.IsNullOrWhiteSpace(dto.Main))
        {
            throw new PluginManifestException("MANIFEST_REGEX", $"{pluginJsonPath}: missing main");
        }

        if (!Version.TryParse(dto.Version, out Version? version))
        {
            throw new PluginManifestException(
                "MANIFEST_REGEX",
                $"{pluginJsonPath}: invalid version '{dto.Version}'");
        }

        if (!Version.TryParse(dto.Api, out Version? apiVersion))
        {
            throw new PluginManifestException(
                "MANIFEST_REGEX",
                $"{pluginJsonPath}: invalid api '{dto.Api}'");
        }

        IReadOnlyList<PluginDependency> depend = ParseDependencies(dto.Depend, pluginJsonPath, required: true);
        IReadOnlyList<PluginSoftDependency> softDepend = ParseSoftDependencies(dto.SoftDepend, pluginJsonPath);

        return new PluginManifest
        {
            Id = dto.Id,
            Version = version,
            ApiVersion = apiVersion,
            Main = dto.Main,
            Depend = depend,
            SoftDepend = softDepend,
            Provides = dto.Provides ?? [],
            DirectoryPath = directory,
            AssemblyPath = ResolveAssemblyPath(directory, dto.Id)
        };
    }

    internal static void ValidateId(string? id, string source)
    {
        if (string.IsNullOrWhiteSpace(id) || !PluginIdRegex().IsMatch(id))
        {
            throw new PluginManifestException(
                "MANIFEST_REGEX",
                $"{source}: invalid plugin id '{id}' (expected prefix:product, [a-z_], segments ≤18)");
        }
    }

    internal static void ValidateFolderName(string folderName, string id, string source)
    {
        if (!string.Equals(folderName, id, StringComparison.Ordinal)
            || !FolderNameRegex().IsMatch(folderName))
        {
            throw new PluginManifestException(
                "MANIFEST_REGEX",
                $"{source}: folder '{folderName}' must match manifest id '{id}'");
        }
    }

    internal static (Version Min, Version Max) ParseVersionRange(
        IReadOnlyList<string>? versions,
        string source,
        string fieldName)
    {
        if (versions is null || versions.Count != 2)
        {
            throw new PluginManifestException(
                "MANIFEST_REGEX",
                $"{source}: {fieldName}.versions must be a two-element SemVer range [min, max]");
        }

        if (!Version.TryParse(versions[0], out Version? min)
            || !Version.TryParse(versions[1], out Version? max)
            || min > max)
        {
            throw new PluginManifestException(
                "MANIFEST_REGEX",
                $"{source}: invalid {fieldName}.versions [{versions[0]}, {versions[1]}]");
        }

        return (min, max);
    }

    static IReadOnlyList<PluginDependency> ParseDependencies(
        List<PluginDependencyDto>? entries,
        string source,
        bool required)
    {
        if (entries is null || entries.Count == 0)
        {
            return [];
        }

        List<PluginDependency> list = [];
        foreach (PluginDependencyDto entry in entries)
        {
            ValidateId(entry.Id, source);
            (Version min, Version max) = ParseVersionRange(entry.Versions, source, $"depend on '{entry.Id}'");
            list.Add(new PluginDependency
            {
                Id = entry.Id!,
                MinVersion = min,
                MaxVersion = max
            });
        }

        return list;
    }

    static IReadOnlyList<PluginSoftDependency> ParseSoftDependencies(
        List<PluginSoftDependencyDto>? entries,
        string source)
    {
        if (entries is null || entries.Count == 0)
        {
            return [];
        }

        List<PluginSoftDependency> list = [];
        foreach (PluginSoftDependencyDto entry in entries)
        {
            ValidateId(entry.Id, source);
            Version min;
            Version max;
            if (entry.Versions is { Count: > 0 })
            {
                (min, max) = ParseVersionRange(entry.Versions, source, $"softdepend on '{entry.Id}'");
            }
            else
            {
                min = new Version(0, 0, 0);
                max = new Version(int.MaxValue, 0, 0);
            }

            PluginSoftLoadOrder load = ParseSoftLoad(entry.Load, source, entry.Id!);
            list.Add(new PluginSoftDependency
            {
                Id = entry.Id!,
                MinVersion = min,
                MaxVersion = max,
                Load = load
            });
        }

        return list;
    }

    static PluginSoftLoadOrder ParseSoftLoad(string? load, string source, string targetId)
    {
        if (string.IsNullOrWhiteSpace(load) || string.Equals(load, "after", StringComparison.OrdinalIgnoreCase))
        {
            return PluginSoftLoadOrder.After;
        }

        if (string.Equals(load, "before", StringComparison.OrdinalIgnoreCase))
        {
            return PluginSoftLoadOrder.Before;
        }

        throw new PluginManifestException(
            "MANIFEST_REGEX",
            $"{source}: softdepend on '{targetId}' has invalid load '{load}' (expected 'before' or 'after')");
    }

    public static string ResolveAssemblyPath(string directory, string pluginId)
    {
        string expectedName = pluginId.Replace(':', '.') + ".dll";
        string expected = Path.Combine(directory, expectedName);
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
            $"Plugin assembly not found for '{pluginId}' (expected {expectedName} in {directory})",
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
        public List<PluginDependencyDto>? Depend { get; set; }

        [JsonPropertyName("softdepend")]
        public List<PluginSoftDependencyDto>? SoftDepend { get; set; }

        [JsonPropertyName("provides")]
        public List<string>? Provides { get; set; }
    }

    private sealed class PluginDependencyDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("versions")]
        public List<string>? Versions { get; set; }
    }

    private sealed class PluginSoftDependencyDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("load")]
        public string? Load { get; set; }

        [JsonPropertyName("versions")]
        public List<string>? Versions { get; set; }
    }
}

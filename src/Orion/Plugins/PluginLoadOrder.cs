using Orion.PluginContracts;

namespace Orion.Plugins;

/// <summary>Deterministic plugin load ordering (manifest v2 depend / softdepend).</summary>
public static class PluginLoadOrder
{
    public static IReadOnlyList<PluginManifest> Sort(IReadOnlyList<PluginManifest> discovered)
    {
        Dictionary<string, PluginManifest> byId = new(StringComparer.Ordinal);
        foreach (PluginManifest manifest in discovered)
        {
            if (!byId.TryAdd(manifest.Id, manifest))
            {
                throw new PluginManifestException(
                    "MANIFEST_REGEX",
                    $"Duplicate plugin id '{manifest.Id}'.");
            }
        }

        ValidateVersionConstraints(byId);
        return TopologicalSort(byId);
    }

    static void ValidateVersionConstraints(Dictionary<string, PluginManifest> byId)
    {
        Dictionary<string, List<(string Requester, Version Min, Version Max)>> constraints =
            new(StringComparer.Ordinal);

        void AddConstraint(string targetId, string requester, Version min, Version max)
        {
            if (!constraints.TryGetValue(targetId, out List<(string, Version, Version)>? list))
            {
                list = [];
                constraints[targetId] = list;
            }

            list.Add((requester, min, max));
        }

        foreach (PluginManifest manifest in byId.Values)
        {
            foreach (PluginDependency dep in manifest.Depend)
            {
                if (!byId.ContainsKey(dep.Id))
                {
                    throw new PluginManifestException(
                        "DEPEND_MISSING",
                        $"Plugin '{manifest.Id}' hard-depends on missing plugin '{dep.Id}'.");
                }

                AddConstraint(dep.Id, manifest.Id, dep.MinVersion, dep.MaxVersion);
            }

            foreach (PluginSoftDependency soft in manifest.SoftDepend)
            {
                if (!byId.TryGetValue(soft.Id, out PluginManifest? target))
                {
                    continue;
                }

                AddConstraint(soft.Id, manifest.Id, soft.MinVersion, soft.MaxVersion);
            }
        }

        foreach ((string targetId, List<(string Requester, Version Min, Version Max)> ranges) in constraints)
        {
            if (!byId.TryGetValue(targetId, out PluginManifest? target))
            {
                continue;
            }

            Version intersectionMin = ranges[0].Min;
            Version intersectionMax = ranges[0].Max;
            foreach ((string _, Version min, Version max) in ranges.Skip(1))
            {
                intersectionMin = min > intersectionMin ? min : intersectionMin;
                intersectionMax = max < intersectionMax ? max : intersectionMax;
                if (intersectionMin > intersectionMax)
                {
                    string detail = string.Join(
                        ", ",
                        ranges.Select(r => $"{r.Requester} [{r.Min}, {r.Max}]"));
                    throw new PluginManifestException(
                        "VERSION_CONSTRAINT_CONFLICT",
                        $"Incompatible version constraints on '{targetId}': {detail}");
                }
            }

            if (target.Version < intersectionMin || target.Version > intersectionMax)
            {
                throw new PluginManifestException(
                    "VERSION_OUT_OF_RANGE",
                    $"Plugin '{targetId}' v{target.Version} is outside required range "
                    + $"[{intersectionMin}, {intersectionMax}]");
            }
        }

        foreach (PluginManifest manifest in byId.Values)
        {
            foreach (PluginDependency dep in manifest.Depend)
            {
                PluginManifest target = byId[dep.Id];
                if (!dep.Contains(target.Version))
                {
                    throw new PluginManifestException(
                        "VERSION_OUT_OF_RANGE",
                        $"Plugin '{manifest.Id}' requires '{dep.Id}' in "
                        + $"[{dep.MinVersion}, {dep.MaxVersion}] but found v{target.Version}");
                }
            }

            foreach (PluginSoftDependency soft in manifest.SoftDepend)
            {
                if (!byId.TryGetValue(soft.Id, out PluginManifest? target))
                {
                    continue;
                }

                if (!soft.Contains(target.Version))
                {
                    throw new PluginManifestException(
                        "VERSION_OUT_OF_RANGE",
                        $"Plugin '{manifest.Id}' soft-depends on '{soft.Id}' in "
                        + $"[{soft.MinVersion}, {soft.MaxVersion}] but found v{target.Version}");
                }
            }
        }
    }

    static IReadOnlyList<PluginManifest> TopologicalSort(Dictionary<string, PluginManifest> byId)
    {
        Dictionary<string, HashSet<string>> outgoing = byId.Keys.ToDictionary(
            id => id,
            _ => new HashSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);
        Dictionary<string, int> indegree = byId.Keys.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);

        void AddEdge(string from, string to)
        {
            if (!outgoing[from].Add(to))
            {
                return;
            }

            indegree[to]++;
        }

        foreach (PluginManifest manifest in byId.Values)
        {
            foreach (PluginDependency dep in manifest.Depend)
            {
                AddEdge(dep.Id, manifest.Id);
            }

            foreach (PluginSoftDependency soft in manifest.SoftDepend)
            {
                if (!byId.ContainsKey(soft.Id))
                {
                    continue;
                }

                if (soft.Load == PluginSoftLoadOrder.Before)
                {
                    AddEdge(manifest.Id, soft.Id);
                }
                else
                {
                    AddEdge(soft.Id, manifest.Id);
                }
            }
        }

        SortedSet<string> ready = new(StringComparer.Ordinal);
        foreach ((string id, int degree) in indegree)
        {
            if (degree == 0)
            {
                ready.Add(id);
            }
        }

        List<PluginManifest> ordered = [];
        while (ready.Count > 0)
        {
            string next = ready.Min!;
            ready.Remove(next);
            ordered.Add(byId[next]);

            foreach (string child in outgoing[next].OrderBy(x => x, StringComparer.Ordinal))
            {
                indegree[child]--;
                if (indegree[child] == 0)
                {
                    ready.Add(child);
                }
            }
        }

        if (ordered.Count != byId.Count)
        {
            string leftover = string.Join(
                ", ",
                indegree.Where(kv => kv.Value > 0).Select(kv => kv.Key).OrderBy(x => x));
            throw new PluginManifestException(
                "ORDER_CYCLE",
                $"Plugin dependency cycle involving: {leftover}");
        }

        return ordered;
    }
}

namespace Orion.Plugins;

/// <summary>Deterministic plugin load ordering (depend / softdepend / loadbefore).</summary>
public static class PluginLoadOrder
{
    public static IReadOnlyList<PluginManifest> Sort(IReadOnlyList<PluginManifest> discovered)
    {
        Dictionary<string, PluginManifest> byId = new(StringComparer.Ordinal);
        foreach (PluginManifest manifest in discovered)
        {
            if (!byId.TryAdd(manifest.Id, manifest))
            {
                throw new InvalidOperationException($"Duplicate plugin id '{manifest.Id}'.");
            }
        }

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
            foreach (string dep in manifest.Depend)
            {
                if (!byId.ContainsKey(dep))
                {
                    throw new InvalidOperationException(
                        $"Plugin '{manifest.Id}' hard-depends on missing plugin '{dep}'.");
                }

                AddEdge(dep, manifest.Id);
            }

            foreach (string soft in manifest.SoftDepend)
            {
                if (byId.ContainsKey(soft))
                {
                    AddEdge(soft, manifest.Id);
                }
            }

            foreach (string other in manifest.LoadBefore)
            {
                if (byId.ContainsKey(other))
                {
                    AddEdge(manifest.Id, other);
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
            throw new InvalidOperationException($"Plugin dependency cycle involving: {leftover}");
        }

        return ordered;
    }
}

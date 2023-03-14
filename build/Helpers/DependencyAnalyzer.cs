using System.Text.Json.Nodes;
using Extensions;

namespace Helpers;

internal static class DependencyAnalyzer
{
    public static DepsJsonDependencyMap BuildDependencyMap(this JsonObject depsJson)
    {
        var dependencyMap = new DepsJsonDependencyMap();

        foreach (var dependencyNode in depsJson.GetDependencies())
        {
            var dependency = dependencyNode.Value.AsObject();
            var dependencyId = dependencyNode.Key.Split('/')[0];

            if (!dependencyMap.TryCreateEntry(dependencyId, isRoot: true))
            {
                var entry = dependencyMap[dependencyId];

                entry.UpdateToRoot();
            }

            foreach (var transientDependencyId in GetTransientDependencies(dependency))
            {
                dependencyMap.TryCreateEntry(transientDependencyId, isRoot: false);

                var entry = dependencyMap[transientDependencyId];
                entry.AddReferee(dependencyId);
            }
        }

        return dependencyMap;
    }

    internal static IList<string> Cleanup(DepsJsonDependencyMap map, string dependencyId)
    {
        if (!map.ContainsKey(dependencyId))
        {
            return Array.Empty<string>();
        }

        var libsToRemove = new List<string>();
        foreach (var dependencyReferee in GetReferees(map, dependencyId))
        {
            if (dependencyReferee.Referees.Count != 1)
            {
                continue;
            }

            libsToRemove.Add(dependencyReferee.Id);
            dependencyReferee.RemoveReferee(dependencyReferee.Id);

            var nestedLibsToRemove = Cleanup(map, dependencyReferee.Id);
            if (nestedLibsToRemove.Any())
            {
                libsToRemove.AddRange(nestedLibsToRemove);
            }
        }

        return libsToRemove;
    }

    internal static IEnumerable<DepsJsonDependency> GetReferees(DepsJsonDependencyMap map, string dependencyId)
    {
        return map
            .Where(x => x.Value.Referees.Contains(dependencyId))
            .Select(x => x.Value)
            .ToList();
    }

    private static IEnumerable<string> GetTransientDependencies(JsonObject transientDependencies)
    {
        if (!transientDependencies.TryGetPropertyValue("dependencies", out var dependenciesNode))
        {
            yield break;
        }

        foreach (var transientDependencyNode in dependenciesNode.AsObject())
        {
            yield return transientDependencyNode.Key;
        }
    }

    private static bool TryCreateEntry(this DepsJsonDependencyMap map, string dependencyId, bool isRoot)
    {
        if (!map.ContainsKey(dependencyId))
        {
            var entry = new DepsJsonDependency()
            {
                Id = dependencyId,
                IsRoot = isRoot,
            };

            map.Add(dependencyId, entry);

            return true;
        }

        return false;
    }

    internal class DepsJsonDependencyMap : Dictionary<string, DepsJsonDependency>
    {
    }

    internal class DepsJsonDependency
    {
        public string Id { get; init; }

        public bool IsRoot { get; set; }

        public HashSet<string> Referees { get; private set; } = new HashSet<string>();

        public void AddReferee(string package)
        {
            Referees.Add(package);
        }

        public void RemoveReferee(string package)
        {
            Referees.Remove(package);
        }

        public void UpdateToRoot()
        {
            IsRoot = true;
        }
    }
}

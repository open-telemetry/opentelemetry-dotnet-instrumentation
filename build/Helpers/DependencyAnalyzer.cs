using System.Text.Json.Nodes;
using Extensions;

namespace Helpers;

internal static class DependencyAnalyzer
{
    public static DepsJsonDependencyMap BuildDependencyMap(this JsonObject depsJson)
    {
        var dependencyMap = new DepsJsonDependencyMap();

        foreach (var dependency in depsJson.GetDependencies())
        {
            var obj = dependency.Value.AsObject();
            var package = dependency.Key.Split('/')[0];

            if (!dependencyMap.TryCreateEntry(package, isRoot: true))
            {
                var entry = dependencyMap[package];

                entry.UpdateToRoot();
            }

            foreach (var transientPackage in GetTransientPackages(obj))
            {
                dependencyMap.TryCreateEntry(transientPackage, isRoot: false);

                var entry = dependencyMap[transientPackage];
                entry.AddReferee(package);
            }
        }

        return dependencyMap;
    }

    internal static List<string> Cleanup(DepsJsonDependencyMap map, string dependency)
    {
        var cleanup = new List<string>();

        if (map.ContainsKey(dependency))
        {
            foreach (var dep in GetReferees(map, dependency))
            {
                if (dep.Count == 1)
                {
                    cleanup.Add(dep.Name);
                    dep.RemoveReferee(dep.Name);

                    var result = Cleanup(map, dep.Name);
                    if (result.Any())
                    {
                        cleanup.AddRange(result);
                    }
                }
            }
        }

        return cleanup;
    }

    internal static IEnumerable<DepsJsonDependency> GetReferees(DepsJsonDependencyMap map, string dependency)
    {
        return map
            .Where(x => x.Value.Referees.Contains(dependency))
            .Select(x => x.Value)
            .ToList();
    }

    private static IEnumerable<string> GetTransientPackages(JsonObject transientDependencies)
    {
        if (transientDependencies.ContainsKey("dependencies"))
        {
            foreach (var transientDependency in transientDependencies["dependencies"].AsObject())
            {
                yield return transientDependency.Key;
            }
        }
    }

    private static bool TryCreateEntry(this Dictionary<string, DepsJsonDependency> map, string package, bool isRoot)
    {
        if (!map.ContainsKey(package))
        {
            var entry = new DepsJsonDependency()
            {
                Name = package,
                IsRoot = isRoot,
            };

            map.Add(package, entry);

            return true;
        }

        return false;
    }

    internal class DepsJsonDependencyMap : Dictionary<string, DepsJsonDependency>
    {
    }

    internal class DepsJsonDependency
    {
        public string Name { get; init; }

        public bool IsRoot { get; set; }

        public HashSet<string> Referees { get; private set; } = new HashSet<string>();

        public int Count => Referees.Count;

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

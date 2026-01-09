using System.Diagnostics;
using System.Reflection;

using var activitySource = new ActivitySource("AssemblyRedirection.NetFramework.ActivitySource");
using var activity = activitySource.StartActivity("AssemblyRedirection.Activity");

Console.WriteLine($"Running {Assembly.GetExecutingAssembly()?.Location}");

var loadedAssemblyNames = new HashSet<string>();
var hasMultipleInstancesOfSingleAssembly = false;
var assemblies = AppDomain.CurrentDomain.GetAssemblies();
Array.Sort<Assembly>(assemblies, static (l, r) => string.CompareOrdinal(l.FullName, r.FullName));
Console.WriteLine("Loaded assemblies:");
foreach (var assembly in assemblies)
{
    var location = assembly.IsDynamic ? "<dynamic>" : assembly.Location;
    Console.WriteLine($" + {assembly.FullName}\n\t{location} ");
    var assemblyName = assembly.GetName()?.Name ?? string.Empty;
    if (!string.IsNullOrWhiteSpace(assemblyName) && !loadedAssemblyNames.Add(assemblyName))
    {
        hasMultipleInstancesOfSingleAssembly = true;
        Console.WriteLine($"\n * WARNING: {assemblyName} loaded more than once.\n");
    }
}

Console.WriteLine();
return hasMultipleInstancesOfSingleAssembly ? -1 : 0;

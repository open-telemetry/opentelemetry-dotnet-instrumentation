// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;

// args[0]: expected assembly name (e.g. "System.Diagnostics.DiagnosticSource").
// args[1]: expected assembly version (e.g. "10.0.0.0").
// args[2]: expected assembly file version (e.g. "10.0.0.0")
// args[3]: comma-separated assembly name patterns to check for duplicates
//          (e.g. "System.Diagnostics.DiagnosticSource,OpenTelemetry.*").
//          Empty or missing means check all assemblies.
//          '-' prefix means exclude pattern (e.g. "-TestApplication.AssemblyRedirection" means check all except TestApplication.AssemblyRedirection).
var expectedAssemblyName = args.Length > 0
    ? args[0]
    : throw new ArgumentException("Missing Expected Assembly Name", nameof(args));

var expectedAssemblyVersion = args.Length > 1
    ? Version.Parse(args[1])
    : throw new ArgumentException($"Missing Expected Assembly Version", nameof(args));

var expectedFileVersion = args.Length > 2
    ? Version.Parse(args[2])
    : throw new ArgumentException($"Missing Expected Assembly File Version", nameof(args));

var duplicateCheckPatterns = args.Length > 3
    ? args[3].Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
    : []; // null = check all

Console.WriteLine("Configuration:");
Console.WriteLine($"  Expected Assembly Name: \"{expectedAssemblyName}\"");
Console.WriteLine($"  Expected Assembly Version: {expectedAssemblyVersion}");
Console.WriteLine($"  Expected Assembly File Version: {expectedFileVersion}");
Console.WriteLine($"  Duplicate check patterns (empty means 'all'): [{string.Join(",", duplicateCheckPatterns)}]");
Console.WriteLine();

using var activitySource = new ActivitySource("AssemblyRedirection.ActivitySource");
using var activity = activitySource.StartActivity("AssemblyRedirection.Activity");

Console.WriteLine("Execution:");
Console.WriteLine($"  Running {Assembly.GetExecutingAssembly()?.Location}");
Console.WriteLine();

// Group by short assembly name so that multiple loads of the same assembly are detected.
var assemblyLookup = AppDomain.CurrentDomain.GetAssemblies()
    .OrderBy(it => it.GetName().Name, StringComparer.OrdinalIgnoreCase)
    .ToLookup(it => it.GetName().Name, StringComparer.OrdinalIgnoreCase);

// Check 1: Verify no assembly matching the patterns is loaded more than once.
// Separate patterns into includes and excludes, validate wildcards
var includes = duplicateCheckPatterns
    .Where(it => !it.StartsWith('-'.ToString(), StringComparison.Ordinal))
    .ToArray();
var excludes = duplicateCheckPatterns
    .Where(it => it.StartsWith('-'.ToString(), StringComparison.Ordinal))
    .Select(it => it.EndsWith('*'.ToString(), StringComparison.Ordinal)
                    ? it.Substring(1)
                    : throw new ArgumentException($"Invalid pattern '{it}': wildcards are only allowed at the end."))
    .ToArray();

var duplicates = assemblyLookup
    .Where(it => it.Count() != 1)
    // filter out excludes
    .Where(it => it.Key is null || excludes.Length == 0 || !MatchPatterns(it.Key, excludes))
    // filter in includes
    .Where(it => it.Key is null || includes.Length == 0 || MatchPatterns(it.Key, includes))
    .Select(it => it.Aggregate($"\"{it.Key}\" loaded multiple times: [{it.Count()}]", (current, next) => $"{current}\n - {Describe(next)}"))
    .ToArray();

if (duplicates.Length > 0)
{
    throw new InvalidOperationException(string.Join("\n", duplicates));
}

Console.WriteLine("Check 1: Duplicate Assemblies");
Console.WriteLine("  Result: No duplicated assemblies");
Console.WriteLine();

// Check 2: Expected assembly must be loaded at the expected version.
var assembly = assemblyLookup[expectedAssemblyName].Single();
Console.WriteLine("Check 2: Assembly Version");
Console.WriteLine($"  Loaded Assembly: {Describe(assembly)}");
var assemblyVersion = assembly.GetName().Version;
if (assembly.GetName().Version != expectedAssemblyVersion)
{
    throw new InvalidOperationException($"Unexpected Assembly Version Loaded: {assemblyVersion}");
}

Console.WriteLine($"  Assembly Version: {assemblyVersion}");
Console.WriteLine();

// Check 3: Expected assembly file must be loaded at the expected file version.
var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
Console.WriteLine("Check 3: Assembly File Version");
Console.WriteLine($"  Assembly File Info:\n{fileVersionInfo}");
if (fileVersionInfo.FileVersion is null || Version.Parse(fileVersionInfo.FileVersion) != expectedFileVersion)
{
    throw new InvalidOperationException($"Unexpected Assembly File Version Loaded: {fileVersionInfo.FileVersion}");
}

Console.WriteLine($"  File Version: {fileVersionInfo.FileVersion}");
Console.WriteLine();

Console.WriteLine("Result: All checks passed.");
return 0;

static bool MatchPatterns(string name, string[] patterns)
{
    return patterns.Any(it => it.EndsWith('*'.ToString(), StringComparison.Ordinal)
            ? name.StartsWith(it.Substring(0, it.Length - 1), StringComparison.OrdinalIgnoreCase)
            : name.Equals(it, StringComparison.OrdinalIgnoreCase));
}

static string Describe(Assembly assembly)
{
    var location = assembly.IsDynamic ? "<dynamic>" : assembly.Location;
    var context =
#if NETFRAMEWORK
        assembly.CodeBase;
#else
        System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly);
#endif
    return $"({assembly})@({context}): \"{location}\"";
}

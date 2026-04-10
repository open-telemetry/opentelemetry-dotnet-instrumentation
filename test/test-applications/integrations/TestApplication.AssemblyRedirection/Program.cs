// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
#if NETFRAMEWORK
using Microsoft.Extensions.Logging.Abstractions;
#endif

// args[0]: expected assembly name (e.g. "System.Diagnostics.DiagnosticSource").
// args[1]: expected assembly version (e.g. "10.0.0.0").
// args[2]: expected assembly file version (e.g. "10.0.0.0")
// args[3]: comma-separated assembly names to exclude from duplicate check
//          (e.g. "TestApplication.AssemblyRedirection").
//          Empty or missing means check all assemblies.
var expectedAssemblyName = args.Length > 0
    ? args[0]
    : throw new ArgumentException("Missing Expected Assembly Name", nameof(args));

var expectedAssemblyVersion = args.Length > 1
    ? Version.Parse(args[1])
    : throw new ArgumentException($"Missing Expected Assembly Version", nameof(args));

var expectedFileVersion = args.Length > 2
    ? Version.Parse(args[2])
    : throw new ArgumentException($"Missing Expected Assembly File Version", nameof(args));

var excludedNames = args.Length > 3
    ? args[3].Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
    : []; // empty = check all assemblies

Console.WriteLine("Configuration:");
Console.WriteLine($"  Expected Assembly Name: \"{expectedAssemblyName}\"");
Console.WriteLine($"  Expected Assembly Version: {expectedAssemblyVersion}");
Console.WriteLine($"  Expected Assembly File Version: {expectedFileVersion}");
Console.WriteLine($"  Excluded Assemblies (empty means 'check all'): [{string.Join(",", excludedNames)}]");
Console.WriteLine();

#if NETFRAMEWORK
// Force an entrypoint dependency that also exists in the auto-instrumentation payload.
var entrypointAssembly = NullLogger.Instance.GetType().Assembly;
Console.WriteLine("Check 0: Entrypoint Assembly");
Console.WriteLine($"  Loaded Assembly: {Describe(entrypointAssembly)}");
Console.WriteLine($"  Assembly Version: {entrypointAssembly.GetName().Version}");
Console.WriteLine();
#endif

using var activitySource = new ActivitySource("AssemblyRedirection.ActivitySource");
using var activity = activitySource.StartActivity("AssemblyRedirection.Activity");

Console.WriteLine("Execution:");
Console.WriteLine($"  Running {Assembly.GetExecutingAssembly()?.Location}");
Console.WriteLine();

// Group by short assembly name so that multiple loads of the same assembly are detected.
var assemblyLookup = AppDomain.CurrentDomain.GetAssemblies()
    .OrderBy(it => it.GetName().Name, StringComparer.OrdinalIgnoreCase)
    .ToLookup(it => it.GetName().Name, StringComparer.OrdinalIgnoreCase);

// Check 1: Verify the assemblies are not loaded twice (use assembly exclude list here).
var duplicates = assemblyLookup
    .Where(it => it.Count() > 1)
    .Where(it => !excludedNames.Contains(it.Key, StringComparer.OrdinalIgnoreCase))
    .OrderBy(it => it.Key, StringComparer.OrdinalIgnoreCase)
    .Select(it => it.Aggregate(
        $"* \"{it.Key}\" loaded [{it.Count()}] times:",
        (current, next) => $"{current}\n    ** {Describe(next)}"))
    .ToArray();

if (duplicates.Length > 0)
{
    throw new InvalidOperationException(
        $"Found [{duplicates.Length}] assemblies loaded > 1:\n{string.Join("\n", duplicates)}");
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
// TODO disabled until a better expected assembly is picked up (less fluctuating to .net runtime version)
/*
var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
Console.WriteLine("Check 3: Assembly File Version");
Console.WriteLine($"  Assembly File Info:\n{fileVersionInfo}");
if (fileVersionInfo.FileVersion is null || Version.Parse(fileVersionInfo.FileVersion) != expectedFileVersion)
{
    throw new InvalidOperationException($"Unexpected Assembly File Version Loaded: {fileVersionInfo.FileVersion}");
}

Console.WriteLine($"  File Version: {fileVersionInfo.FileVersion}");
Console.WriteLine();
*/

Console.WriteLine("Result: All checks passed.");
return 0;

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

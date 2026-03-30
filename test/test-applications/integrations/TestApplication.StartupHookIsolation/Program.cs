// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

// Args: [exit: <int>|throw] [exceptionType] [exceptionMessage]
// Modes:
//   <int>       - returns <int> exit code
//   throw       - throws an exception of the provided exceptionType with the provided exceptionMessage
// This application is doing the following:
//  1. Sends a span with the name of the AssemblyLoadContext where the executing assembly is loaded
//  2. Checks that the Main entrypoint is not running twice by using a process-wide environment variable as a marker
//  3. Exits:
//      3.1. Throws the provided exception, or
//      3.2. With the provided exit code within range [1,99], or
//      3.3. With 0 if application is running in Default ALC or 999 otherwise

var exit = args.Length > 0 ? args[0] : null;
var exceptionType = args.Length > 1 ? args[1] : null;
var exceptionMessage = args.Length > 2 ? string.Join(" ", args.Skip(2)) : null;

// 1. Sends a span with the name of the AssemblyLoadContext where the executing assembly is loaded
var currentAlc = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!;
using var activitySource = new ActivitySource("StartupHookIsolation.ActivitySource");
using var activity = activitySource.StartActivity(currentAlc.Name!);

// 2. Checks that the Main entrypoint is not running twice by using a process-wide environment variable as a marker
const string EnvVarName = "TEST_ENTRYPOINT_CALLED";
if (Environment.GetEnvironmentVariable(EnvVarName) is not null)
{
    throw new InvalidOperationException("FAIL: Double execution detected!");
}

Console.WriteLine($"[FIRST_EXECUTION] Setup environment variable {EnvVarName}");
Environment.SetEnvironmentVariable(EnvVarName, "true");

// 3. Exits:
// 3.1. Throws the provided exception
if (exit == "throw" && exceptionType is not null && exceptionMessage is not null)
{
    Console.WriteLine($"[MODE] Throw mode - will throw {exceptionType} with message: \"{exceptionMessage}\"");
    throw (Exception)Activator.CreateInstance(Type.GetType(exceptionType)!, [exceptionMessage])!;
}

// 3.2. With the provided exit code within range [1,99]
if (int.TryParse(exit, out var exitCode))
{
    // double check the custom exit code is within expected range to avoid accidental exits with special code (0, 999)
    if (exitCode is not > 0 or not < 100)
    {
        throw new ArgumentException($"Exit Code should be within range [1, 99]: {exitCode}");
    }

    Console.WriteLine($"[MODE] Success mode - will return exit code {exitCode}");
    return exitCode;
}

// 3.3. With 0 if application is running in Default ALC or 999 otherwise
return currentAlc == AssemblyLoadContext.Default ? 0 : 999;

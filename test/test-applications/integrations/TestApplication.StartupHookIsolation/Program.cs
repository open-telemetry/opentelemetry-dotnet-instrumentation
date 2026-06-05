// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

// This application validates StartupHook isolation with different entry point signatures:
//   - Void: static void Main(string[] args)
//   - Int: static int Main(string[] args)
//   - Task: static Task Main(string[] args)
//   - TaskInt: static Task<int> Main(string[] args)
//   - AsyncTask: static async Task Main(string[] args)
//   - AsyncTaskInt: static async Task<int> Main(string[] args)
//
// Args: [exit: <int>|throw] [exceptionType] [exceptionMessage]
// Modes:
//   <int>       - returns <int> exit code (ignored for void/Task entry points)
//   throw       - throws an exception of the provided exceptionType with the provided exceptionMessage
//   (empty)     - returns 999 if running in Default ALC, 0 if running in other ALC
// This application is doing the following:
//  1. Sends a span with the name of the AssemblyLoadContext where the executing assembly is loaded
//  2. Checks that the Main entrypoint is not running twice by using a process-wide environment variable as a marker
//  3. Exits:
//      3.1. Throws the provided exception, or
//      3.2. With the provided exit code within range [1,99], or
//      3.3. With 999 if application is running in Default ALC or 0 in isolated ALC

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

internal sealed class Program
{
#if VOID_MAIN
    internal static void Main(string[] args)
    {
        Console.WriteLine("[ENTRYPOINT] static void Main(string[] args)");
        ExecuteLogic(args);
    }
#elif INT_MAIN
    internal static int Main(string[] args)
    {
        Console.WriteLine("[ENTRYPOINT] static int Main(string[] args)");
        return ExecuteLogic(args);
    }
#elif TASK_MAIN
    internal static Task Main(string[] args)
    {
        Console.WriteLine("[ENTRYPOINT] static Task Main(string[] args)");
        return Task.Delay(100).ContinueWith(_ => { ExecuteLogic(args); }, TaskScheduler.Default);
    }
#elif TASK_INT_MAIN
    internal static Task<int> Main(string[] args)
    {
        Console.WriteLine("[ENTRYPOINT] static Task<int> Main(string[] args)");
        return Task.Delay(100).ContinueWith(_ => ExecuteLogic(args), TaskScheduler.Default);
    }
#elif ASYNC_TASK_MAIN
    internal static async Task Main(string[] args)
    {
        Console.WriteLine("[ENTRYPOINT] static async Task Main(string[] args)");
        await Task.Delay(100).ConfigureAwait(false);
        ExecuteLogic(args);
    }
#elif ASYNC_TASK_INT_MAIN
    internal static async Task<int> Main(string[] args)
    {
        Console.WriteLine("[ENTRYPOINT] static async Task<int> Main(string[] args)");
        await Task.Delay(100).ConfigureAwait(false);
        return ExecuteLogic(args);
    }
#else
#error Entry point type not set or invalid. Expected build configuration for one of: Void, Int, Task, TaskInt, AsyncTask, or AsyncTaskInt
#endif

    private static int ExecuteLogic(string[] args)
    {
        var exit = args.Length > 0 ? args[0] : null;
        var exceptionTypeName = args.Length > 1 ? args[1] : null;
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
        if (exit == "throw" && exceptionTypeName is not null && exceptionMessage is not null)
        {
            Console.WriteLine($"[MODE] Throw mode - will throw {exceptionTypeName} with message: \"{exceptionMessage}\"");
            var exceptionType = Type.GetType(exceptionTypeName)!;
            var exception = (Exception)Activator.CreateInstance(exceptionType, [exceptionMessage])!;
            throw exception;
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

        // 3.3. With 999 if application is running in Default ALC or 0 in isolated ALC
        return currentAlc == AssemblyLoadContext.Default ? 999 : 0;
    }
}

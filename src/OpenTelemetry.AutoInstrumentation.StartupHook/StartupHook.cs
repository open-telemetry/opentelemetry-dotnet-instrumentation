// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation;
using OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Dotnet StartupHook
/// </summary>
internal class StartupHook
{
    /// <summary>
    /// Load and initialize OpenTelemetry.AutoInstrumentation assembly to bring OpenTelemetry SDK
    /// with a pre-defined set of exporters, shims, and instrumentations.
    /// </summary>
    public static void Initialize()
    {
        // ASSEMBLY RESOLUTION STRATEGY
        //
        // === STARTUP HOOK ONLY ===
        // Without the native profiler's IL rewriting and NuGet's build-time resolution,
        // the customer application and its dependencies automatically load into the
        // Default ALC. The instrumentation cannot override versions there or prevent
        // customer code from loading first. To solve this, we hijack the application:
        // load its entry assembly into an isolated ALC alongside our dependencies,
        // execute customer entrypoint in the isolated ALC, then exit to prevent
        // the Default ALC copy from running.
        //
        // When the customer application loads into the isolated ALC, all its dependencies
        // are automatically tried to be loaded to the same ALC.
        // In this process the first place we can hook in is the isolated ALC's Load() method.
        // This is our single control point for version resolution.
        // For each dependency, the isolated ALC compares the TPA version against the
        // instrumentation version and picks the higher one. Before loading, it validates
        // that the selected version >= the requested version. If the best available
        // version is still lower than requested, the isolated ALC skips the request
        // rather than loading an incompatible assembly.
        //
        // === AGGRESSIVE ISOLATION (TRAMPOLINE) ===
        // To keep the Default ALC free of agent dependencies (logging, rules, Loader),
        // the isolation decision is made with minimal type references, and the rest
        // of the setup is performed in isolated ALC to guarantee that the type resolution
        // starts from there.
        //
        // If isolation setup fails, we revert the context and control returns to
        // the .NET runtime, which falls back to normal execution (or fail-fast
        // if configured).
        //
        // NoInlining on each branch prevents the JIT from pulling type references from the
        // "other" branch into Initialize(). Without it, the JIT could resolve types like
        // OtelLogging when Initialize() is compiled, defeating the lazy loading boundary.
        //
        // Logging is intentionally deferred until inside InitializationSetup.Run() for the
        // same reason - it triggers assembly loads that must happen in the correct ALC.
        if (IsStartupHookOnlyMode() && IsRedirectEnabled())
        {
            BootstrapIsolation();
        }
        else
        {
            BootstrapNormal();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void BootstrapNormal()
    {
        new NormalSetup().Run();
    }

    /// <summary>
    /// Trampoline into isolated execution: create the isolated ALC, load a second copy
    /// of this StartupHook assembly into it, and invoke IsolatedSetup.Run() via reflection.
    /// From that point forward, all type resolution happens through the isolated ALC.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void BootstrapIsolation()
    {
        // create isolated ALC
        var isolatedContext = new IsolatedAssemblyLoadContext();

        // execute the rest of setup within the isolated ALC
        var setupType = isolatedContext.IsolatedAssembly.GetType(typeof(IsolatedSetup).FullName!)!;
        var setup = Activator.CreateInstance(setupType, [isolatedContext, isolatedContext.IsolatedReflectionScope]);
        var runMethod = setupType.GetMethod(nameof(IsolatedSetup.Run), BindingFlags.Instance | BindingFlags.Public)!;
        // DoNotWrapExceptions preserves original exception types from both
        // instrumentation failures (fail-fast) and customer application entrypoint failures.
        runMethod.Invoke(setup, BindingFlags.DoNotWrapExceptions, null, null, null);
    }

    private static bool IsStartupHookOnlyMode()
    {
        return Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.ProfilerEnabledVariable) != "1";
    }

    private static bool IsRedirectEnabled()
    {
        var envValue = Environment.GetEnvironmentVariable(ConfigurationKeys.RedirectEnabled);
        if (bool.TryParse(envValue, out var redirectEnabled))
        {
            return redirectEnabled;
        }

        // Not explicitly set: default based on deployment type - true for standalone, false otherwise.
        // For non-standalone deployments, assembly resolution is handled at build time,
        // so assembly redirection is not considered.
        return DeploymentDetector.IsStandaloneDeployment();
    }
}

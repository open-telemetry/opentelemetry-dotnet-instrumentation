// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.Loader;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Initialization for StartupHook-only deployment (no native profiler).
/// Runs inside the isolated AssemblyLoadContext — instantiated via reflection trampoline
/// from <see cref="StartupHook"/> so that all agent assembly loads (logging, rules, Loader)
/// resolve through <see cref="IsolatedAssemblyLoadContextHelper.Load"/> instead of Default ALC.
/// See <see cref="IsolatedAssemblyLoadContextHelper"/> for the version resolution strategy.
/// </summary>
internal sealed class IsolatedSetup(AssemblyLoadContext isolatedContext, AssemblyLoadContext.ContextualReflectionScope contextualReflectionScope) : InitializationSetup
{
    private Assembly? _originalEntryAssembly;
    private MethodInfo? _entryPoint;
    private object[]? _entryPointParameters;

    protected override string ModeName => "Isolation";

    protected override void Initialize(string instrumentationHomePath)
    {
        GetTargetApp(out var targetAppPath, out _originalEntryAssembly);

        Logger.Debug($"Isolation mode - Target app path: {targetAppPath}");
        Logger.Debug($"Isolation mode - Target entry assembly: {_originalEntryAssembly}");

        // Load customer entry assembly into isolated context
        var isolatedEntryAssembly = isolatedContext.LoadFromAssemblyPath(targetAppPath);
        Logger.Debug($"Loaded target entry assembly to isolated context: {isolatedEntryAssembly.FullName}");

        // Initialize instrumentation loader (contextual reflection is active)
        var loaderAssembly = Assembly.Load(LoaderAssemblyName)
            ?? throw new InvalidOperationException("Failed to load Loader");
        _ = loaderAssembly.CreateInstance(LoaderTypeName)
            ?? throw new InvalidOperationException("Failed to create an instance of the Loader");

        // Set entry assembly for frameworks and third-party that depends on it
        Assembly.SetEntryAssembly(isolatedEntryAssembly);
        Logger.Debug("Set assembly loaded in isolated context as entry assembly");

        // Resolve entrypoint
        _entryPoint = isolatedEntryAssembly.EntryPoint
            ?? throw new InvalidOperationException("Entry assembly has no entrypoint");
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        _entryPointParameters = _entryPoint.GetParameters().Length > 0 ? [args] : null;
        Logger.Debug("Ready to execute entry assembly in isolated context");
    }

    /// <summary>
    /// Reverts the isolation setup. Called if initialization fails,
    /// allowing the runtime to fall back to normal execution.
    /// </summary>
    protected override void OnError(Exception ex)
    {
        // Revert contextual reflection
        contextualReflectionScope.Dispose();

        // Restore original entry assembly
        if (_originalEntryAssembly != null)
        {
            try
            {
                Assembly.SetEntryAssembly(_originalEntryAssembly);
            }
            catch
            {
                // Best effort - may fail if runtime state has changed
            }
        }
    }

    /// <summary>
    /// Executes the customer application entrypoint in the isolated context.
    /// Called after successful setup and logger cleanup.
    /// </summary>
    protected override void AfterInitialize()
    {
        // We deliberately do not handle exceptions from the customer entrypoint.
        // Customer application failures are not an instrumentation concern and should
        // propagate as unhandled exceptions, terminating the process naturally.
        var exitCode = _entryPoint!.Invoke(null, BindingFlags.DoNotWrapExceptions, null, _entryPointParameters, null) as int? ?? 0;
        Environment.Exit(exitCode);
    }

    private static void GetTargetApp(out string appPath, out Assembly? entryAssembly)
    {
        // Try entry assembly first (should already be loaded in Default ALC)
        entryAssembly = Assembly.GetEntryAssembly();
        if (!string.IsNullOrEmpty(entryAssembly?.Location))
        {
            appPath = entryAssembly.Location;
            return;
        }

        // Fallback: try command line args
        Logger.Warning("Entry assembly location is unavailable, falling back to command line parsing. This may indicate an unexpected runtime scenario.");

        var args = Environment.GetCommandLineArgs();
        if (args.Length > 0 &&
            (args[0].EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
             args[0].EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) &&
            File.Exists(args[0]))
        {
            appPath = Path.GetFullPath(args[0]);
            return;
        }

        throw new InvalidOperationException(
            "Cannot determine target application path. " +
            "GetEntryAssembly().Location is empty and GetCommandLineArgs()[0] is not a valid assembly.");
    }
}

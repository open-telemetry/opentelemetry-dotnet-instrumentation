// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.Loader;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Provides assembly resolution for StartupHook-only deployment (no native profiler).
/// Creates an isolated AssemblyLoadContext that loads both customer and agent assemblies,
/// </summary>
internal sealed class IsolatedAssemblyResolver(string targetAppPath, Assembly? originalEntryAssembly, IOtelLogger logger) : IDisposable
{
    private AssemblyLoadContext.ContextualReflectionScope? _contextualReflectionScope;
    private Assembly? _isolatedEntryAssembly;
    private IsolatedAssemblyLoadContext? _isolatedContext;

    /// <summary>
    /// Sets up an isolated AssemblyLoadContext, initializes instrumentation,
    /// and executes the customer application entrypoint.
    /// </summary>
    /// <returns>The exit code from the customer's Main method, or 0 if void.</returns>
    public int Run()
    {
        logger.Debug($"Isolation mode - Target app path: {targetAppPath}");
        logger.Debug($"Isolation mode - Target entry assembly: {originalEntryAssembly}");

        // 1. Create isolated context
        _isolatedContext = new IsolatedAssemblyLoadContext();
        logger.Debug("Created isolated AssemblyLoadContext");

        // 2. Enable contextual reflection (helps with Assembly.Load(string), Type.GetType)
        _contextualReflectionScope = _isolatedContext.EnterContextualReflection();
        logger.Debug("Contextual Reflection is set to isolated context");

        // 3. Load customer entry assembly into isolated context
        _isolatedEntryAssembly = _isolatedContext.LoadFromAssemblyPath(targetAppPath);
        logger.Debug($"Loaded target entry assembly to isolated context: {_isolatedEntryAssembly.FullName}");

        // 4. Initialize instrumentation loader (contextual reflection is active)
        var loaderAssembly = Assembly.Load(StartupHook.LoaderAssemblyName)
            ?? throw new InvalidOperationException("Failed to load Loader");
        _ = loaderAssembly.CreateInstance(StartupHook.LoaderTypeName)
            ?? throw new InvalidOperationException("Failed to create an instance of the Loader");

        // 5. Set entry assembly for frameworks and third-party that depends on it
        Assembly.SetEntryAssembly(_isolatedEntryAssembly);
        logger.Debug("Set entry assembly to target assembly loaded in isolated context");

        // 6. Execute customer entrypoint
        var entryPoint = _isolatedEntryAssembly.EntryPoint
            ?? throw new InvalidOperationException("Entry assembly has no entrypoint");

        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        var parameters = entryPoint.GetParameters().Length > 0 ? new object[] { args } : null;

        return entryPoint.Invoke(null, parameters) as int? ?? 0;
    }

    /// <summary>
    /// Reverts the isolation setup. Called automatically if an exception occurs during setup
    /// (before customer entrypoint is invoked), allowing the runtime to fall back to normal execution.
    /// </summary>
    public void Dispose()
    {
        // Revert contextual reflection
        _contextualReflectionScope?.Dispose();

        // Restore original entry assembly
        if (originalEntryAssembly != null)
        {
            try
            {
                Assembly.SetEntryAssembly(originalEntryAssembly);
            }
            catch
            {
                // Best effort - may fail if runtime state has changed
            }
        }
    }
}

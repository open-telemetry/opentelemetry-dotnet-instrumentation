// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.Loader;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Orchestrates isolation setup for StartupHook-only deployment (no native profiler).
/// Creates an isolated AssemblyLoadContext, loads customer and agent assemblies into it,
/// and initializes instrumentation. Can revert the setup if initialization fails.
/// </summary>
internal sealed class IsolationSetup(string targetAppPath, Assembly? originalEntryAssembly, IOtelLogger logger)
{
    private AssemblyLoadContext.ContextualReflectionScope? _contextualReflectionScope;
    private IsolatedAssemblyLoadContext? _isolatedContext;
    private MethodInfo? _entryPoint;
    private object[]? _entryPointParameters;

    /// <summary>
    /// Sets up an isolated AssemblyLoadContext and initializes instrumentation.
    /// If this method throws, call <see cref="Revert"/> to undo the isolation setup
    /// and allow the runtime to fall back to normal execution.
    /// </summary>
    public void Setup()
    {
        logger.Debug($"Isolation mode - Target app path: {targetAppPath}");
        logger.Debug($"Isolation mode - Target entry assembly: {originalEntryAssembly}");

        // 1. Create isolated context
        _isolatedContext = new IsolatedAssemblyLoadContext();
        logger.Debug("Created isolated AssemblyLoadContext");

        // 2. Enable contextual reflection (helps with Assembly.Load(string), Type.GetType)
        _contextualReflectionScope = _isolatedContext.EnterContextualReflection();
        logger.Debug("Set isolated context as CurrentContextualReflectionContext");

        // 3. Load customer entry assembly into isolated context
        var isolatedEntryAssembly = _isolatedContext.LoadFromAssemblyPath(targetAppPath);
        logger.Debug($"Loaded target entry assembly to isolated context: {isolatedEntryAssembly.FullName}");

        // 4. Initialize instrumentation loader (contextual reflection is active)
        var loaderAssembly = Assembly.Load(StartupHook.LoaderAssemblyName)
            ?? throw new InvalidOperationException("Failed to load Loader");
        _ = loaderAssembly.CreateInstance(StartupHook.LoaderTypeName)
            ?? throw new InvalidOperationException("Failed to create an instance of the Loader");

        // 5. Set entry assembly for frameworks and third-party that depends on it
        Assembly.SetEntryAssembly(isolatedEntryAssembly);
        logger.Debug("Set assembly loaded in isolated context as entry assembly");

        // 6. Resolve entrypoint
        _entryPoint = isolatedEntryAssembly.EntryPoint
            ?? throw new InvalidOperationException("Entry assembly has no entrypoint");
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        _entryPointParameters = _entryPoint.GetParameters().Length > 0 ? [args] : null;
        logger.Debug("Ready to execute entry assembly in isolated context");
    }

    /// <summary>
    /// Executes the customer application entrypoint in the isolated context.
    /// Must be called after <see cref="Setup"/> succeeds.
    /// </summary>
    /// <returns>The exit code from the customer's Main method, or 0 if void.</returns>
    public int InvokeEntryPoint()
    {
        // use DoNotWrapExceptions to preserve the original exception type and stack trace if the entrypoint throws
        return _entryPoint!.Invoke(null, BindingFlags.DoNotWrapExceptions, null, _entryPointParameters, null) as int? ?? 0;
    }

    /// <summary>
    /// Reverts the isolation setup. Should be called if <see cref="Setup"/> fails,
    /// allowing the runtime to fall back to normal execution.
    /// </summary>
    public void Revert()
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

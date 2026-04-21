// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Normal initialization for deployments with the native profiler.
/// Loads the Loader assembly directly via Assembly.LoadFrom.
/// </summary>
internal sealed class NormalSetup : InitializationSetup
{
    protected override string ModeName => "Normal";

    protected override void Initialize(string instrumentationHomePath)
    {
        // With Native profiler, we load the Loader from instrumentation home path,
        // create an instance of OpenTelemetry.AutoInstrumentation.Loader.Loader
        // which will setup assembly resolution and initialize Instrumentation
        var loaderFilePath = Path.Combine(instrumentationHomePath, $"{LoaderAssemblyName}.dll");
        var loaderAssembly = Assembly.LoadFrom(loaderFilePath)
            ?? throw new InvalidOperationException("Failed to load Loader assembly");
        _ = loaderAssembly.CreateInstance(LoaderTypeName)
            ?? throw new InvalidOperationException("Failed to create an instance of the Loader");
    }
}

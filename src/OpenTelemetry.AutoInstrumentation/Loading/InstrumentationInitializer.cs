// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Loading;

/// <summary>
/// InstrumentationInitializer encapsulates instrumentation initialization
/// together with the assemblies which are required by the implementation.
/// </summary>
internal abstract class InstrumentationInitializer
{
    protected InstrumentationInitializer(string requiredAssemblyName, string initializerName)
    {
        RequiredAssemblyName = requiredAssemblyName;
        InitializerName = initializerName;
    }

    public string RequiredAssemblyName { get; }

    public string InitializerName { get; }

    public abstract void Initialize(ILifespanManager lifespanManager);
}

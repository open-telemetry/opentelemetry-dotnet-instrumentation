// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Loading;

internal class GenericInitializer : InstrumentationInitializer
{
    private readonly Action<ILifespanManager> _initialize;

    public GenericInitializer(string assemblyName, Action<ILifespanManager> initialize)
        : base(assemblyName)
    {
        _initialize = initialize;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        _initialize(lifespanManager);
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class AspNetMvcInitializer : InstrumentationInitializer
{
    private readonly Action<ILifespanManager> _initialize;

    public AspNetMvcInitializer(Action<ILifespanManager> initialize, string initializerName)
        : base("System.Web.Mvc", initializerName)
    {
        _initialize = initialize;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        _initialize(lifespanManager);
    }
}

#endif

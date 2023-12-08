// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal class AspNetWebApiInitializer : InstrumentationInitializer
{
    private readonly Action<ILifespanManager> _initialize;

    public AspNetWebApiInitializer(Action<ILifespanManager> initialize)
        : base("System.Web.Http")
    {
        _initialize = initialize;
    }

    public override void Initialize(ILifespanManager lifespanManager)
    {
        _initialize(lifespanManager);
    }
}

#endif

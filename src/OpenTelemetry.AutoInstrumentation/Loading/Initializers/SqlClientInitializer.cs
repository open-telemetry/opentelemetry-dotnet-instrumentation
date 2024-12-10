// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal abstract class SqlClientInitializer
{
    protected SqlClientInitializer(LazyInstrumentationLoader lazyInstrumentationLoader)
    {
        lazyInstrumentationLoader.Add(new GenericInitializer("System.Data.SqlClient", InitializeOnFirstCall));
        lazyInstrumentationLoader.Add(new GenericInitializer("Microsoft.Data.SqlClient", InitializeOnFirstCall));

#if NETFRAMEWORK
        lazyInstrumentationLoader.Add(new GenericInitializer("System.Data", InitializeOnFirstCall));
#endif
    }

    protected abstract void InitializeOnFirstCall(ILifespanManager lifespanManager);
}

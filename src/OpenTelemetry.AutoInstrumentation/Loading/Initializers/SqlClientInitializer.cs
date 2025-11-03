// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal abstract class SqlClientInitializer
{
    protected SqlClientInitializer(LazyInstrumentationLoader lazyInstrumentationLoader, string initializerNamePrefix)
    {
        lazyInstrumentationLoader.Add(new GenericInitializer("System.Data.SqlClient", $"{initializerNamePrefix}ForSystemDataSqlClient", InitializeOnFirstCall));
        lazyInstrumentationLoader.Add(new GenericInitializer("Microsoft.Data.SqlClient", $"{initializerNamePrefix}ForMicrosoftDataSqlClient", InitializeOnFirstCall));

#if NETFRAMEWORK
        lazyInstrumentationLoader.Add(new GenericInitializer("System.Data", $"{initializerNamePrefix}ForSystemData", InitializeOnFirstCall));
#endif
    }

    protected abstract void InitializeOnFirstCall(ILifespanManager lifespanManager);
}

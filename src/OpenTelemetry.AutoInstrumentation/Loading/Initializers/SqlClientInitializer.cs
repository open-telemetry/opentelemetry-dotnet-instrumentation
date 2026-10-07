// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal abstract class SqlClientInitializer
{
    private readonly string _initializerNamePrefix;

    protected SqlClientInitializer(string initializerNamePrefix)
    {
        _initializerNamePrefix = initializerNamePrefix;
    }

    public void Register(LazyInstrumentationLoader lazyInstrumentationLoader)
    {
        lazyInstrumentationLoader.Add(new GenericInitializer("System.Data.SqlClient", $"{_initializerNamePrefix}ForSystemDataSqlClient", InitializeOnFirstCall));
        lazyInstrumentationLoader.Add(new GenericInitializer("Microsoft.Data.SqlClient", $"{_initializerNamePrefix}ForMicrosoftDataSqlClient", InitializeOnFirstCall));

#if NETFRAMEWORK
        lazyInstrumentationLoader.Add(new GenericInitializer("System.Data", $"{_initializerNamePrefix}ForSystemData", InitializeOnFirstCall));
#endif
    }

    protected abstract void InitializeOnFirstCall(ILifespanManager lifespanManager);
}

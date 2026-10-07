// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Loading.Initializers;

internal abstract class SqlClientInitializer : IInstrumentationInitializer
{
    private readonly string _initializerNamePrefix;

    protected SqlClientInitializer(string initializerNamePrefix)
    {
        _initializerNamePrefix = initializerNamePrefix;
    }

    public void Register(LazyInstrumentationLoader lazyInstrumentationLoader)
    {
        new GenericInitializer("System.Data.SqlClient", $"{_initializerNamePrefix}ForSystemDataSqlClient", InitializeOnFirstCall).Register(lazyInstrumentationLoader);
        new GenericInitializer("Microsoft.Data.SqlClient", $"{_initializerNamePrefix}ForMicrosoftDataSqlClient", InitializeOnFirstCall).Register(lazyInstrumentationLoader);

#if NETFRAMEWORK
        new GenericInitializer("System.Data", $"{_initializerNamePrefix}ForSystemData", InitializeOnFirstCall).Register(lazyInstrumentationLoader);
#endif
    }

    protected abstract void InitializeOnFirstCall(ILifespanManager lifespanManager);
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Instrumentation options
/// </summary>
internal class InstrumentationOptions
{
    internal InstrumentationOptions(Configuration configuration)
    {
        GraphQLSetDocument = configuration.GetBool(ConfigurationKeys.Traces.InstrumentationOptions.GraphQLSetDocument) ?? false;
        SqlClientSetDbStatementForText = configuration.GetBool(ConfigurationKeys.Traces.InstrumentationOptions.SqlClientSetDbStatementForText) ?? false;
#if NET6_0_OR_GREATER
        EntityFrameworkCoreSetDbStatementForText = configuration.GetBool(ConfigurationKeys.Traces.InstrumentationOptions.EntityFrameworkCoreSetDbStatementForText) ?? false;
#endif
    }

    /// <summary>
    /// Gets a value indicating whether GraphQL query can be passed as a Document tag.
    /// </summary>
    public bool GraphQLSetDocument { get; }

    /// <summary>
    /// Gets a value indicating whether text query in SQL Client can be passed as a db.statement tag.
    /// </summary>
    public bool SqlClientSetDbStatementForText { get; }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Gets a value indicating whether text query in Entity Framework Core can be passed as a db.statement tag.
    /// </summary>
    public bool EntityFrameworkCoreSetDbStatementForText { get; }
#endif
}

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
    }

    /// <summary>
    /// Gets a value indicating whether GraphQL query can be passed as a Document tag.
    /// </summary>
    public bool GraphQLSetDocument { get; }
}

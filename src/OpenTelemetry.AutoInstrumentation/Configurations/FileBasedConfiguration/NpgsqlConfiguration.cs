// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class NpgsqlConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the Npgsql instrumentation should propagate trace context to PostgreSQL.
    /// </summary>
    [YamlMember(Alias = "context_propagation")]
    public bool ContextPropagation { get; set; }
}

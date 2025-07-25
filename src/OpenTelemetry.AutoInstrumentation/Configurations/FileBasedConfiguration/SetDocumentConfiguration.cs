// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class SetDocumentConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the GraphQL instrumentation can pass raw queries through the graphql.document attribute.
    /// Queries might contain sensitive information.
    /// </summary>
    [YamlMember(Alias = "set_document")]
    public bool SetDocument { get; set; }
}

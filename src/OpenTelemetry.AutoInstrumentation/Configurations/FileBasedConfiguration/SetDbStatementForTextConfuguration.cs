// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

[EmptyObjectOnEmptyYaml]
internal class SetDbStatementForTextConfuguration
{
    /// <summary>
    /// Gets or sets a value indicating whether SQL statements are passed through the db.statement attribute.
    /// If false, db.statement is recorded only for executing stored procedures.
    /// </summary>
    [YamlMember(Alias = "set_db_statement_for_text")]
    public bool SetDbStatementForText { get; set; }
}

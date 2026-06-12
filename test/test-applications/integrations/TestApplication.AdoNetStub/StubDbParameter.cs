// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data;
using System.Data.Common;
#if NET
using System.Diagnostics.CodeAnalysis;
#endif

namespace TestApplication.AdoNetStub;

internal sealed class StubDbParameter : DbParameter
{
    public override DbType DbType { get; set; } = DbType.String;

    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;

    public override bool IsNullable { get; set; }

#if NET
    [AllowNull]
#endif
    public override string ParameterName { get; set; } = string.Empty;

    public override int Size { get; set; }

#if NET
    [AllowNull]
#endif
    public override string SourceColumn { get; set; } = string.Empty;

    public override bool SourceColumnNullMapping { get; set; }

    public override object? Value { get; set; }

    public override void ResetDbType() => DbType = DbType.String;
}

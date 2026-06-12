// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data;
using System.Data.Common;
#if NET
using System.Diagnostics.CodeAnalysis;
#endif

namespace TestApplication.AdoNetStub;

internal sealed class StubDbCommand : DbCommand
{
#if NET
    [AllowNull]
#endif
    public override string CommandText { get; set; } = string.Empty;

    public override int CommandTimeout { get; set; } = 30;

    public override CommandType CommandType { get; set; } = CommandType.Text;

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get; set; }

    protected override DbTransaction? DbTransaction { get; set; }

    protected override DbParameterCollection DbParameterCollection { get; } = new StubDbParameterCollection();

    public override void Cancel()
    {
    }

    public override int ExecuteNonQuery()
    {
        return 0;
    }

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }

    public override object? ExecuteScalar()
    {
        return null;
    }

    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<object?>(null);
    }

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter() => new StubDbParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        return new StubDbDataReader();
    }

    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        return Task.FromResult<DbDataReader>(new StubDbDataReader());
    }
}

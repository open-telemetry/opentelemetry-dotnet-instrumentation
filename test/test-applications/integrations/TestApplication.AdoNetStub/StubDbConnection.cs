// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data;
using System.Data.Common;
#if true
using System.Diagnostics.CodeAnalysis;
#endif

namespace TestApplication.AdoNetStub;

internal sealed class StubDbConnection : DbConnection
{
    private ConnectionState _state = ConnectionState.Closed;

#if NET
    [AllowNull]
#endif
    public override string ConnectionString { get; set; } = string.Empty;

    public override string Database => "FakeDatabase";

    public override string DataSource => "FakeDataSource";

    public override string ServerVersion => "1.0.0";

    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName)
    {
    }

    public override void Close() => _state = ConnectionState.Closed;

    public override void Open() => _state = ConnectionState.Open;

    protected override DbCommand CreateDbCommand() => new StubDbCommand { Connection = this };

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        new StubDbTransaction(this, isolationLevel);
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data;
using System.Data.Common;

namespace TestApplication.AdoNetStub;

internal sealed class StubDbTransaction(StubDbConnection connection, IsolationLevel isolationLevel) : DbTransaction
{
    public override IsolationLevel IsolationLevel { get; } = isolationLevel;

    protected override DbConnection DbConnection { get; } = connection;

    public override void Commit()
    {
    }

    public override void Rollback()
    {
    }
}

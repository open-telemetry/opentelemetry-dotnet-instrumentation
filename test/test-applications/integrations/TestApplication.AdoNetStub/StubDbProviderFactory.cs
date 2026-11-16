// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Data.Common;

namespace TestApplication.AdoNetStub;

internal sealed class StubDbProviderFactory(Task? executionGate) : DbProviderFactory
{
    public override DbConnection CreateConnection() => new StubDbConnection { ExecutionGate = executionGate };
}

#endif

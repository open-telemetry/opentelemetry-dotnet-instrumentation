// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using TestApplication.AdoNetStub;
using TestApplication.Shared;

ConsoleHelper.WriteSplashScreen(args);

using var connection = new StubDbConnection();
await connection.OpenAsync().ConfigureAwait(false);

#pragma warning disable CA1849 // Intentionally calling both sync and async variants to ensure both are properly traced.

// Sync: ExecuteNonQuery  →  instruments DbCommand.ExecuteNonQuery
using (var command = connection.CreateCommand())
{
    command.CommandText = "INSERT INTO FakeTable VALUES (1, 'value1')";
    var affected = command.ExecuteNonQuery();
    Console.WriteLine($"ExecuteNonQuery affected rows: {affected}");
}

// Sync: ExecuteScalar  →  instruments DbCommand.ExecuteScalar
using (var command = connection.CreateCommand())
{
    command.CommandText = "SELECT COUNT(*) FROM FakeTable";
    var count = command.ExecuteScalar();
    Console.WriteLine($"ExecuteScalar result: {count}");
}

// Sync: ExecuteReader  →  instruments DbCommand.ExecuteDbDataReader
using (var command = connection.CreateCommand())
{
    command.CommandText = "SELECT * FROM FakeTable";
    using var reader = command.ExecuteReader();
    Console.WriteLine($"ExecuteReader HasRows: {reader.HasRows}");
}

#pragma warning restore CA1849

// Async: ExecuteNonQueryAsync  →  instruments DbCommand.ExecuteNonQueryAsync
using (var command = connection.CreateCommand())
{
    command.CommandText = "INSERT INTO FakeTable VALUES (2, 'value2')";
    var affected = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    Console.WriteLine($"ExecuteNonQueryAsync affected rows: {affected}");
}

// Async: ExecuteScalarAsync  →  instruments DbCommand.ExecuteScalarAsync
using (var command = connection.CreateCommand())
{
    command.CommandText = "SELECT COUNT(*) FROM FakeTable";
    var count = await command.ExecuteScalarAsync().ConfigureAwait(false);
    Console.WriteLine($"ExecuteScalarAsync result: {count}");
}

// Async: ExecuteReaderAsync  →  instruments DbCommand.ExecuteDbDataReaderAsync
using (var command = connection.CreateCommand())
{
    command.CommandText = "SELECT * FROM FakeTable";
    using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
    Console.WriteLine($"ExecuteReaderAsync HasRows: {reader.HasRows}");
}

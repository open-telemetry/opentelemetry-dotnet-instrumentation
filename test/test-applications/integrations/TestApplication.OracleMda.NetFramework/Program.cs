// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Oracle.ManagedDataAccess.Client;
using TestApplication.Shared;

ConsoleHelper.WriteSplashScreen(args);

var oraclePort = ArgumentHelper.GetArgument(args, "--port", "1521");
var oracleUser = ArgumentHelper.GetArgument(args, "--user", "appuser");
var oraclePassword = ArgumentHelper.GetRequiredArgument(args, "--password");
var oracleDataSource = ArgumentHelper.GetArgument(args, "--data-source", $"localhost:{oraclePort}/FREEPDB1");

using var connection = new OracleConnection($"User Id={oracleUser};Password={oraclePassword};Data Source={oracleDataSource};");

connection.Open();

using var command = new OracleCommand("SELECT 1", connection);
using var oracleDataReader = command.ExecuteReader();
while (oracleDataReader.Read())
{
    Console.WriteLine("Result: " + oracleDataReader[0]);
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Oracle.ManagedDataAccess.Client;
using TestApplication.Shared;

ConsoleHelper.WriteSplashScreen(args);

var oraclePort = GeOraclePort(args);
var oraclePassword = GetOraclePassword(args);

using var connection = new OracleConnection($"User Id=appuser;Password={oraclePassword};Data Source=localhost:{oraclePort}/FREEPDB1;");

connection.Open();

using var command = new OracleCommand("SELECT 1", connection);
using var oracleDataReader = command.ExecuteReader();
while (oracleDataReader.Read())
{
    Console.WriteLine("Result: " + oracleDataReader[0]);
}

static string GeOraclePort(string[] args)
{
    if (args.Length > 1)
    {
        return args[1];
    }

    return "1521";
}

static string GetOraclePassword(string[] args)
{
    if (args.Length > 3)
    {
        return args[3];
    }

    throw new NotSupportedException("Lack of password for the Oracle.");
}

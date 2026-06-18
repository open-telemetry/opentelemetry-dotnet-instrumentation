// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data;
using System.Globalization;
using System.Text;

namespace TestApplication.Shared;

internal static class SqlServerScalarCommandExecutor
{
    public const string CommandTextArgument = "--command-text";
    public const string EnableTransactionArgument = "--enable-transaction";

    private const string CommandResultOutputPrefix = "CommandResult=";

    public static bool TryExecute(IDbConnection connection, string[] args)
    {
        var commandText = ArgumentHelper.GetArgument(args, CommandTextArgument, string.Empty);
        if (string.IsNullOrEmpty(commandText))
        {
            return false;
        }

        IDbTransaction? transaction = null;
        try
        {
            connection.Open();

            if (ArgumentHelper.HasArgument(args, EnableTransactionArgument))
            {
                transaction = connection.BeginTransaction();
            }

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities. The query is static test code supplied by integration tests.
            command.CommandText = commandText;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities.
            command.Transaction = transaction;

            var commandResult = command.ExecuteScalar();
            transaction?.Commit();
            WriteCommandResult(commandResult);
        }
        finally
        {
            transaction?.Dispose();
        }

        return true;
    }

    private static void WriteCommandResult(object? commandResult)
    {
        var output = commandResult switch
        {
            byte[] bytes => Encoding.ASCII.GetString(bytes).TrimEnd('\0'),
            DBNull => string.Empty,
            null => string.Empty,
            _ => Convert.ToString(commandResult, CultureInfo.InvariantCulture)
        };

        Console.WriteLine($"{CommandResultOutputPrefix}{output}");
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using Npgsql;
using TestApplication.Shared;

namespace TestApplication.Npgsql;

internal static class Program
{
    private const string ContextPropagationApplicationName = "otel-context-probe";
    private const string StaleContextApplicationName = "otel-stale-probe";
    private const string MultiplexingApplicationName = "otel-multiplexing-probe";
    private const string UntracedMarker = "/* untraced */";

    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var postgresPort = ArgumentHelper.GetArgument(args, "--postgres", "5432");

        var connString = $"Server=127.0.0.1;Port={postgresPort};User ID=postgres";

        if (ArgumentHelper.HasArgument(args, "--stale-context-scenario"))
        {
            await RunStaleContextScenarioAsync(connString).ConfigureAwait(false);
            return;
        }

        if (ArgumentHelper.HasArgument(args, "--multiplexing-context-scenario"))
        {
            await RunMultiplexingContextScenarioAsync(connString).ConfigureAwait(false);
            return;
        }

        var contextPropagationScenario = ArgumentHelper.HasArgument(args, "--context-propagation-scenario");
        if (contextPropagationScenario)
        {
            connString += $";Application Name={ContextPropagationApplicationName}";
        }

        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync().ConfigureAwait(false);

        if (contextPropagationScenario)
        {
            RunSynchronousCommand(conn);
            await RunParameterizedReaderAsync(conn).ConfigureAwait(false);
            await RunNonQueryAsync(conn).ConfigureAwait(false);
            await RunPreparedCommandAsync(conn).ConfigureAwait(false);
            await RunTransactionalCommandAsync(conn).ConfigureAwait(false);
            await RunBatchAsync(conn).ConfigureAwait(false);
            if (GetNpgsqlMajorVersion() >= 10)
            {
                await RunCopyExportAsync(conn, "COPY (SELECT current_setting('application_name')) TO STDOUT", WriteApplicationName).ConfigureAwait(false);
            }

            WriteValue(
                "ContextPostOperationApplicationName",
                await ReadApplicationNameForBackendAsync(connString, conn.ProcessID).ConfigureAwait(false));

            return;
        }

        if (ArgumentHelper.HasArgument(args, "--collector-context-propagation-scenario"))
        {
            await RunCollectorContextPropagationScenarioAsync(conn).ConfigureAwait(false);
            return;
        }

        using var cmd = new NpgsqlCommand(@"SELECT 123;", conn);
        using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            Console.WriteLine(reader.GetInt32(0));
        }
    }

    private static void RunSynchronousCommand(NpgsqlConnection connection)
    {
        using var command = new NpgsqlCommand("SHOW application_name", connection);
        WriteApplicationName(command.ExecuteScalar());
    }

    private static async Task RunParameterizedReaderAsync(NpgsqlConnection connection)
    {
        using var command = new NpgsqlCommand("SELECT current_setting('application_name') WHERE @value = 1", connection);
        command.Parameters.AddWithValue("value", 1);
        using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        if (!await reader.ReadAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException("The parameterized Npgsql command did not return a row.");
        }

        WriteApplicationName(reader.GetString(0));
    }

    private static async Task RunNonQueryAsync(NpgsqlConnection connection)
    {
        using (var command = new NpgsqlCommand("SELECT set_config('otel.test_context', current_setting('application_name'), false)", connection))
        {
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        using var readbackCommand = new NpgsqlCommand("SELECT current_setting('otel.test_context')", connection);
        WriteApplicationName(await readbackCommand.ExecuteScalarAsync().ConfigureAwait(false));
    }

    private static async Task RunPreparedCommandAsync(NpgsqlConnection connection)
    {
        using var command = new NpgsqlCommand("SELECT current_setting('application_name')", connection);
        await command.PrepareAsync().ConfigureAwait(false);
        WriteApplicationName(await command.ExecuteScalarAsync().ConfigureAwait(false));
    }

    private static async Task RunTransactionalCommandAsync(NpgsqlConnection connection)
    {
#if NETFRAMEWORK
        using var transaction = connection.BeginTransaction();
#else
        using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);
#endif
        using var command = new NpgsqlCommand("SELECT current_setting('application_name') /* transaction */", connection, transaction);
        WriteApplicationName(await command.ExecuteScalarAsync().ConfigureAwait(false));
        await transaction.CommitAsync().ConfigureAwait(false);
    }

    private static async Task RunBatchAsync(NpgsqlConnection connection)
    {
        using var batch = new NpgsqlBatch(connection);
        batch.BatchCommands.Add(new NpgsqlBatchCommand("SELECT current_setting('application_name')"));
        batch.BatchCommands.Add(new NpgsqlBatchCommand("SELECT 1"));
        using var reader = await batch.ExecuteReaderAsync().ConfigureAwait(false);
        if (!await reader.ReadAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException("The Npgsql batch did not return a row.");
        }

        WriteApplicationName(reader.GetString(0));
    }

    private static async Task RunStaleContextScenarioAsync(string connectionString)
    {
        using var dataSource = CreateDataSourceWithTracingFilters(
            $"{connectionString};Application Name={StaleContextApplicationName};Maximum Pool Size=1;No Reset On Close=true");

        int tracedBackendProcessId;
        using (var connection = CreateConnection(dataSource))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            using var command = new NpgsqlCommand("SELECT current_setting('application_name'), pg_backend_pid()", connection);
            var result = await ReadApplicationNameAndBackendProcessIdAsync(command).ConfigureAwait(false);
            WriteValue("StaleTracedApplicationName", result.ApplicationName);
            tracedBackendProcessId = result.BackendProcessId;
            WriteValue(
                "StalePostOperationApplicationName",
                await ReadApplicationNameForBackendAsync(connectionString, tracedBackendProcessId).ConfigureAwait(false));
        }

        int untracedBackendProcessId;
        using (var connection = CreateConnection(dataSource))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            using var command = new NpgsqlCommand(
                $"SELECT current_setting('application_name'), pg_backend_pid() {UntracedMarker}",
                connection);
            var result = await ReadApplicationNameAndBackendProcessIdAsync(command).ConfigureAwait(false);
            WriteValue("StaleUntracedApplicationName", result.ApplicationName);
            untracedBackendProcessId = result.BackendProcessId;
        }

        WriteValue("StaleTracedBackendProcessId", tracedBackendProcessId);
        WriteValue("StaleUntracedBackendProcessId", untracedBackendProcessId);

        if (GetNpgsqlMajorVersion() >= 10)
        {
            int tracedCopyBackendProcessId;
            using (var connection = CreateConnection(dataSource))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var result = await ReadCopyApplicationNameAndBackendProcessIdAsync(
                        connection,
                        "COPY (SELECT current_setting('application_name'), pg_backend_pid()) TO STDOUT")
                    .ConfigureAwait(false);
                WriteValue("StaleTracedCopyApplicationName", result.ApplicationName);
                tracedCopyBackendProcessId = result.BackendProcessId;
                WriteValue(
                    "StalePostCopyApplicationName",
                    await ReadApplicationNameForBackendAsync(connectionString, tracedCopyBackendProcessId).ConfigureAwait(false));
            }

            int untracedCopyBackendProcessId;
            using (var connection = CreateConnection(dataSource))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var result = await ReadCopyApplicationNameAndBackendProcessIdAsync(
                        connection,
                        $"COPY (SELECT current_setting('application_name'), pg_backend_pid() {UntracedMarker}) TO STDOUT")
                    .ConfigureAwait(false);
                WriteValue("StaleUntracedCopyApplicationName", result.ApplicationName);
                untracedCopyBackendProcessId = result.BackendProcessId;
            }

            WriteValue("StaleTracedCopyBackendProcessId", tracedCopyBackendProcessId);
            WriteValue("StaleUntracedCopyBackendProcessId", untracedCopyBackendProcessId);
        }

        await RunFailedTransactionStaleContextScenarioAsync(dataSource, connectionString).ConfigureAwait(false);
    }

    private static async Task RunFailedTransactionStaleContextScenarioAsync(IDisposable dataSource, string connectionString)
    {
        int failedTransactionBackendProcessId;
        using (var connection = CreateConnection(dataSource))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            failedTransactionBackendProcessId = connection.ProcessID;
            using var transaction = await BeginTransactionAsync(connection).ConfigureAwait(false);
            try
            {
                using var command = new NpgsqlCommand("SELECT 1 / 0", connection, transaction);
                await command.ExecuteScalarAsync().ConfigureAwait(false);
                throw new InvalidOperationException("The command expected to fail completed successfully.");
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.DivisionByZero)
            {
            }

            await transaction.RollbackAsync().ConfigureAwait(false);
            WriteValue(
                "FailedTransactionPostRollbackApplicationName",
                await ReadApplicationNameForBackendAsync(connectionString, failedTransactionBackendProcessId).ConfigureAwait(false));
        }

        using (var connection = CreateConnection(dataSource))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            using var command = new NpgsqlCommand(
                $"SELECT current_setting('application_name'), pg_backend_pid() {UntracedMarker}",
                connection);
            var result = await ReadApplicationNameAndBackendProcessIdAsync(command).ConfigureAwait(false);
            WriteValue("FailedTransactionUntracedApplicationName", result.ApplicationName);
            WriteValue("FailedTransactionBackendProcessId", failedTransactionBackendProcessId);
            WriteValue("FailedTransactionUntracedBackendProcessId", result.BackendProcessId);
        }
    }

    private static async Task RunCollectorContextPropagationScenarioAsync(NpgsqlConnection connection)
    {
        using (var command = new NpgsqlCommand(
                   "SELECT current_setting('application_name') FROM pg_sleep(0.75)",
                   connection))
        {
            WriteValue("CollectorApplicationName", await command.ExecuteScalarAsync().ConfigureAwait(false));
        }

        using (var transaction = await BeginTransactionAsync(connection).ConfigureAwait(false))
        {
            using var command = new NpgsqlCommand(
                "SELECT current_setting('application_name') FROM pg_sleep(0.75) /* collector transaction */",
                connection,
                transaction);
            WriteValue("CollectorTransactionApplicationName", await command.ExecuteScalarAsync().ConfigureAwait(false));
            await transaction.CommitAsync().ConfigureAwait(false);
        }

        if (GetNpgsqlMajorVersion() >= 10)
        {
            await RunCopyExportAsync(
                    connection,
                    "COPY (SELECT current_setting('application_name') FROM pg_sleep(0.75)) TO STDOUT",
                    value => WriteValue("CollectorCopyApplicationName", value))
                .ConfigureAwait(false);
        }
    }

    private static async Task RunMultiplexingContextScenarioAsync(string connectionString)
    {
        const int commandCount = 20;
        var multiplexingConnectionString =
            $"{connectionString};Multiplexing=true;Maximum Pool Size=1;Application Name={MultiplexingApplicationName}";

        var commands = Enumerable.Range(0, commandCount).Select(async _ =>
        {
            using var connection = new NpgsqlConnection(multiplexingConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            using var command = new NpgsqlCommand("SELECT current_setting('application_name')", connection);
            return await command.ExecuteScalarAsync().ConfigureAwait(false);
        });

        var applicationNames = await Task.WhenAll(commands).ConfigureAwait(false);
        foreach (var applicationName in applicationNames)
        {
            WriteValue("MultiplexedApplicationName", applicationName);
        }
    }

    private static async Task RunCopyExportAsync(NpgsqlConnection connection, string commandText, Action<object?> writeValue)
    {
        using var reader = await connection.BeginTextExportAsync(commandText).ConfigureAwait(false);
        writeValue((await reader.ReadToEndAsync().ConfigureAwait(false)).Trim());
    }

    private static async Task<(string ApplicationName, int BackendProcessId)> ReadApplicationNameAndBackendProcessIdAsync(
        NpgsqlCommand command)
    {
        using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        if (!await reader.ReadAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException("The Npgsql command did not return an application name and backend process ID.");
        }

        return (reader.GetString(0), reader.GetInt32(1));
    }

    private static async Task<(string ApplicationName, int BackendProcessId)> ReadCopyApplicationNameAndBackendProcessIdAsync(
        NpgsqlConnection connection,
        string commandText)
    {
        using var reader = await connection.BeginTextExportAsync(commandText).ConfigureAwait(false);
        var fields = (await reader.ReadToEndAsync().ConfigureAwait(false)).Trim().Split('\t');
        if (fields.Length != 2 || !int.TryParse(fields[1], out var backendProcessId))
        {
            throw new InvalidOperationException("The Npgsql COPY operation did not return an application name and backend process ID.");
        }

        return (fields[0], backendProcessId);
    }

    private static async Task<string> ReadApplicationNameForBackendAsync(string connectionString, int backendProcessId)
    {
        using var observerConnection = new NpgsqlConnection(connectionString);
        await observerConnection.OpenAsync().ConfigureAwait(false);
        using var command = new NpgsqlCommand(
            "SELECT application_name FROM pg_stat_activity WHERE pid = @backendProcessId",
            observerConnection);
        command.Parameters.AddWithValue("backendProcessId", backendProcessId);
        return await command.ExecuteScalarAsync().ConfigureAwait(false) as string
               ?? throw new InvalidOperationException($"PostgreSQL backend {backendProcessId} was not found.");
    }

    private static async Task<NpgsqlTransaction> BeginTransactionAsync(NpgsqlConnection connection)
    {
#if NETFRAMEWORK
        return connection.BeginTransaction();
#else
        return await connection.BeginTransactionAsync().ConfigureAwait(false);
#endif
    }

    private static IDisposable CreateDataSourceWithTracingFilters(string connectionString)
    {
        var builderType = typeof(NpgsqlConnection).Assembly.GetType("Npgsql.NpgsqlDataSourceBuilder", throwOnError: true)!;
        var builder = Activator.CreateInstance(builderType, connectionString)
                      ?? throw new InvalidOperationException("Could not create NpgsqlDataSourceBuilder.");
        var configureTracingMethod = builderType.GetMethod("ConfigureTracing", BindingFlags.Instance | BindingFlags.Public)
                                     ?? throw new InvalidOperationException("This Npgsql version does not support tracing filters.");
        var optionsType = configureTracingMethod.GetParameters()[0].ParameterType.GetGenericArguments()[0];
        var configureMethod = typeof(Program)
                                  .GetMethod(nameof(ConfigureTracingOptions), BindingFlags.Static | BindingFlags.NonPublic)!
                                  .MakeGenericMethod(optionsType);
        var configureDelegate = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(optionsType), configureMethod);
        _ = configureTracingMethod.Invoke(builder, [configureDelegate]);

        var dataSource = builderType.GetMethod("Build", BindingFlags.Instance | BindingFlags.Public)!.Invoke(builder, null);
        return dataSource as IDisposable
               ?? throw new InvalidOperationException("Could not build an Npgsql data source.");
    }

    private static NpgsqlConnection CreateConnection(IDisposable dataSource)
    {
        return dataSource.GetType().GetMethod("CreateConnection", BindingFlags.Instance | BindingFlags.Public)!.Invoke(dataSource, null) as NpgsqlConnection
               ?? throw new InvalidOperationException("Could not create an Npgsql connection.");
    }

    private static void ConfigureTracingOptions<TOptions>(TOptions options)
    {
        var optionsType = typeof(TOptions);
        var commandFilter = new Func<NpgsqlCommand, bool>(command => command.CommandText.IndexOf(UntracedMarker, StringComparison.Ordinal) < 0);
        _ = optionsType.GetMethod("ConfigureCommandFilter", BindingFlags.Instance | BindingFlags.Public)!.Invoke(options, [commandFilter]);

        if (optionsType.GetMethod("ConfigureCopyOperationFilter", BindingFlags.Instance | BindingFlags.Public) is { } configureCopyFilter)
        {
            var copyFilter = new Func<string, bool>(commandText => commandText.IndexOf(UntracedMarker, StringComparison.Ordinal) < 0);
            _ = configureCopyFilter.Invoke(options, [copyFilter]);
        }
    }

    private static int GetNpgsqlMajorVersion()
    {
        return typeof(NpgsqlConnection).Assembly.GetName().Version?.Major ?? 0;
    }

    private static void WriteApplicationName(object? value)
    {
        WriteValue("ApplicationName", value);
    }

    private static void WriteValue(string name, object? value)
    {
        Console.WriteLine($"{name}={value}");
    }
}

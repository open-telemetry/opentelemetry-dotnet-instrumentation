// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using static IntegrationTests.Helpers.DockerFileHelper;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class OracleCollectionFixture : ICollectionFixture<OracleFixture>
{
    public const string Name = nameof(OracleCollectionFixture);
}

public class OracleFixture : IAsyncLifetime
{
    private const int OraclePort = 1522;
    private const string OracleAdminUser = "ADMIN";
    private const string OracleDatabaseServiceName = "myatp_low";
    private const string OracleClientServiceName = "myatp_low";
    private const string OracleWalletDirectory = "/u01/app/oracle/wallets/tls_wallet";
    private const string OracleOpenTelemetryTraceEndpoint = "http://127.0.0.1:4318/v1/traces";
    private static readonly string OracleImage = ReadImageFrom("oracle.Dockerfile");
    private static readonly string[] WalletFiles =
    [
        "adb_container.cert",
        "cwallet.sso",
        "ewallet.p12",
        "ewallet.pem",
        "keystore.jks",
        "ojdbc.properties",
        "README",
        "sqlnet.ora",
        "tnsnames.ora",
        "truststore.jks"
    ];

    private IContainer? _container;
    private bool _openTelemetryTracingConfigured;

    public OracleFixture()
    {
        if (IsCurrentArchitectureSupported)
        {
            Port = TcpPortProvider.GetOpenPort();
        }
    }

    public bool IsCurrentArchitectureSupported { get; } = EnvironmentTools.IsX64();

    public int Port { get; }

    public string User { get; } = OracleAdminUser;

    public string Password { get; } = CreateOraclePassword();

    public string DataSource { get; } = OracleClientServiceName;

    public string WalletDirectory { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        if (!IsCurrentArchitectureSupported)
        {
            return;
        }

        _container = await LaunchOracleContainerAsync(Port, Password).ConfigureAwait(false);
        await WaitForOracleDatabaseAsync(_container, Password).ConfigureAwait(false);
        WalletDirectory = await CopyWalletAsync(_container, Port).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownOracleContainerAsync(_container).ConfigureAwait(false);
        }

        DeleteWalletDirectory();
    }

    public void SkipIfUnsupportedPlatform()
    {
        if (!IsCurrentArchitectureSupported)
        {
            throw new SkipException("Oracle is supported only on AMD64.");
        }
    }

    public async Task EnableOpenTelemetryTracingAsync()
    {
        if (_openTelemetryTracingConfigured)
        {
            return;
        }

        if (_container == null)
        {
            throw new InvalidOperationException("Oracle container is not running.");
        }

        var result = await _container.ExecAsync(CreateOpenTelemetryTracingConfigurationCommand(Password)).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to enable Oracle OpenTelemetry tracing. Exit code: {result.ExitCode}. Stdout: {result.Stdout} Stderr: {result.Stderr}");
        }

        _openTelemetryTracingConfigured = true;
    }

    public async Task AssertTraceContextArchivedAsync(string traceIdHex, string parentSpanIdHex)
    {
        if (_container == null)
        {
            throw new InvalidOperationException("Oracle container is not running.");
        }

        var deadline = DateTime.UtcNow.Add(TestTimeout.Expectation);
        var command = CreateTraceContextSearchCommand(traceIdHex, parentSpanIdHex);

        while (DateTime.UtcNow < deadline)
        {
            var result = await _container.ExecAsync(command).ConfigureAwait(false);
            if (result.ExitCode == 0)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"Timed out waiting for Oracle to archive propagated trace context traceid-{traceIdHex}:parentid-{parentSpanIdHex}.");
    }

    private static async Task ShutdownOracleContainerAsync(IContainer container)
    {
        await container.DisposeAsync().ConfigureAwait(false);
    }

    private static string[] CreateOpenTelemetryTracingConfigurationCommand(string password)
    {
        var traceEndpoint = EscapeSqlLiteral(OracleOpenTelemetryTraceEndpoint);
        var sql = @"
set define off
set serveroutput on
whenever sqlerror exit sql.sqlcode

begin
  dbms_observability.enable_service;
  dbms_observability.enable_service_option(option_id => dbms_observability.capture_traces);
  dbms_observability.add_endpoint(
    endpoint_type => dbms_observability.otel_traces,
    endpoint => '" + traceEndpoint + @"',
    credential_name => NULL);
  dbms_observability.enable_endpoint(endpoint => '" + traceEndpoint + @"');
end;
/
exit
";

        return CreateSqlPlusCommand(sql, password);
    }

    private static string[] CreateTraceContextSearchCommand(string traceIdHex, string parentSpanIdHex)
    {
        return ["bash", "-lc", $"grep -R -E 'traceid-{traceIdHex}.*parentid-{parentSpanIdHex}' /u01/app/oracle/diag/rdbms/*/*/trace/*_dt*.trc 2>/dev/null"];
    }

    private static async Task<IContainer> LaunchOracleContainerAsync(int port, string password)
    {
        var containersBuilder = new ContainerBuilder(OracleImage)
            .WithEnvironment("WORKLOAD_TYPE", "ATP")
            .WithEnvironment("ADMIN_PASSWORD", password)
            .WithEnvironment("WALLET_PASSWORD", password)
            .WithName($"oracle-adb-{port}")
            .WithPrivileged(true)
            .WithPortBinding(port, OraclePort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(OraclePort));

        var container = containersBuilder.Build();
        await container.StartAsync().ConfigureAwait(false);

        return container;
    }

    private static async Task WaitForOracleDatabaseAsync(IContainer container, string password)
    {
        var sql = @"
set heading off feedback off pages 0
whenever sqlerror exit sql.sqlcode
select 1 from dual;
exit
";
        var deadline = DateTime.UtcNow.AddMinutes(10);
        var command = CreateSqlPlusCommand(sql, password);

        while (DateTime.UtcNow < deadline)
        {
            var result = await container.ExecAsync(command).ConfigureAwait(false);
            if (result.ExitCode == 0)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }

        throw new InvalidOperationException("Timed out waiting for Oracle ADB to accept SQLPlus connections.");
    }

    private static async Task<string> CopyWalletAsync(IContainer container, int port)
    {
        var walletDirectory = Path.Combine(Path.GetTempPath(), "oracle-adb-wallets", port.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(walletDirectory);

        foreach (var fileName in WalletFiles)
        {
            var bytes = await container.ReadFileAsync($"{OracleWalletDirectory}/{fileName}").ConfigureAwait(false);
            await WriteAllBytesAsync(Path.Combine(walletDirectory, fileName), bytes).ConfigureAwait(false);
        }

        RewriteWalletNetworkConfiguration(walletDirectory, port);

        return walletDirectory;
    }

    private static void RewriteWalletNetworkConfiguration(string walletDirectory, int port)
    {
        var tnsNamesPath = Path.Combine(walletDirectory, "tnsnames.ora");
        var tnsNames = ReplaceOrdinal(
            File.ReadAllText(tnsNamesPath, Encoding.UTF8),
            "(port=1522)",
            $"(port={port.ToString(CultureInfo.InvariantCulture)})");
        File.WriteAllText(tnsNamesPath, tnsNames, Encoding.UTF8);

        var sqlNet = $@"WALLET_LOCATION =
  (SOURCE =
    (METHOD = FILE)
    (METHOD_DATA =
      (DIRECTORY = {walletDirectory})
    )
  )
SSL_SERVER_DN_MATCH = no
";
        File.WriteAllText(Path.Combine(walletDirectory, "sqlnet.ora"), sqlNet, Encoding.UTF8);
    }

    private static string[] CreateSqlPlusCommand(string sql, string password)
    {
        return ["bash", "-lc", $"export TNS_ADMIN={OracleWalletDirectory}; cat <<'SQL' | sqlplus -s {OracleAdminUser}/{password}@{OracleDatabaseServiceName}\n{sql}\nSQL"];
    }

    private static string CreateOraclePassword()
    {
        return $"Otel1A{Guid.NewGuid():N}".Substring(0, 24);
    }

    private static string EscapeSqlLiteral(string value)
    {
        return ReplaceOrdinal(value, "'", "''");
    }

    private static string ReplaceOrdinal(string value, string oldValue, string newValue)
    {
        if (oldValue.Length == 0)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        var startIndex = 0;

        while (true)
        {
            var matchIndex = value.IndexOf(oldValue, startIndex, StringComparison.Ordinal);
            if (matchIndex < 0)
            {
                builder.Append(value, startIndex, value.Length - startIndex);
                return builder.ToString();
            }

            builder.Append(value, startIndex, matchIndex - startIndex);
            builder.Append(newValue);
            startIndex = matchIndex + oldValue.Length;
        }
    }

    private static async Task WriteAllBytesAsync(string path, byte[] bytes)
    {
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
#if NET
        await fileStream.WriteAsync(bytes).ConfigureAwait(false);
#else
        await fileStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
#endif
    }

    private void DeleteWalletDirectory()
    {
        if (string.IsNullOrEmpty(WalletDirectory) || !Directory.Exists(WalletDirectory))
        {
            return;
        }

        try
        {
            Directory.Delete(WalletDirectory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup for temporary wallet files.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort cleanup for temporary wallet files.
        }
    }
}

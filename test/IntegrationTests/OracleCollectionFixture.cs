// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Net;
using System.Text;
#if NET
using System.Formats.Asn1;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
#endif
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using static IntegrationTests.Helpers.DockerFileHelper;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class OracleCollectionFixture : ICollectionFixture<OracleFixture>
{
    public const string Name = nameof(OracleCollectionFixture);
}

public sealed class OracleFixture : IAsyncLifetime, IDisposable
{
    private const int OraclePort = 1522;
    private const int OracleContainerStartAttempts = 2;
    private const string OracleAdminUser = "ADMIN";
    private const string OracleDatabaseServiceName = "myatp_low";
    private const string OracleClientServiceName = "myatp_low";
    private const string OracleWalletDirectory = "/u01/app/oracle/wallets/tls_wallet";
    private const string OracleEntrypointPath = "/u01/scripts/entrypoint-with-kstrc.sh";
    private const string OracleOpenTelemetryTraceHost = "host.docker.internal";
    private const UnixFileModes ExecutableFileMode = UnixFileModes.UserRead | UnixFileModes.UserWrite | UnixFileModes.UserExecute | UnixFileModes.GroupRead | UnixFileModes.GroupExecute | UnixFileModes.OtherRead | UnixFileModes.OtherExecute;

    // Overall budget for making the Oracle container ready (container start + DB accepting
    // SQL*Plus). Kept safely below the integration tests' `--blame-hang 10m` timeout so that a
    // container that never becomes ready fails this collection fixture fast and locally,
    // instead of hanging until the hang-dump timer aborts the entire test run.
    private static readonly TimeSpan OracleReadinessBudget = TimeSpan.FromMinutes(8);
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

#if NET
    private readonly HashSet<string> _configuredOpenTelemetryTraceEndpoints = [];
    private readonly OracleDatabaseOpenTelemetryCrlServer _databaseOpenTelemetryCrlServer;
#endif

    private IContainer? _container;
    private bool _openTelemetryTracingConfigured;
    private bool _disposed;
#if NET
    private bool _openTelemetryWalletConfigured;
#endif

    public OracleFixture()
    {
        if (IsCurrentArchitectureSupported)
        {
            Port = TcpPortProvider.GetOpenPort();
        }

#if NET
        _databaseOpenTelemetryCrlServer = new OracleDatabaseOpenTelemetryCrlServer();
        DatabaseOpenTelemetryCertificate = OracleDatabaseOpenTelemetryCertificate.Create(OracleOpenTelemetryTraceHost, _databaseOpenTelemetryCrlServer.Port);
        _databaseOpenTelemetryCrlServer.SetCrlPem(DatabaseOpenTelemetryCertificate.RootCertificateCrlPem);
#endif
    }

    public bool IsCurrentArchitectureSupported { get; } = EnvironmentTools.IsX64();

    public int Port { get; }

    public string User { get; } = OracleAdminUser;

    public string Password { get; } = CreateOraclePassword();

    public string DataSource { get; } = OracleClientServiceName;

    public string WalletDirectory { get; private set; } = string.Empty;

#if NET
    internal OracleDatabaseOpenTelemetryCertificate DatabaseOpenTelemetryCertificate { get; }

#endif

    public async Task InitializeAsync()
    {
        if (!IsCurrentArchitectureSupported)
        {
            return;
        }

        using var readinessCts = new CancellationTokenSource(OracleReadinessBudget);

        _container = await LaunchOracleContainerAsync(Port, Password, readinessCts.Token).ConfigureAwait(false);
        await WaitForOracleDatabaseAsync(_container, Password, readinessCts.Token).ConfigureAwait(false);
        WalletDirectory = await CopyWalletAsync(_container, Port).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownOracleContainerAsync(_container).ConfigureAwait(false);
        }

        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

#if NET
        _databaseOpenTelemetryCrlServer.Dispose();
        DatabaseOpenTelemetryCertificate.Dispose();
#endif

        DeleteWalletDirectory();
        GC.SuppressFinalize(this);
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

#if NET
    public async Task EnableOpenTelemetryTracingAsync(int collectorPort)
    {
        if (_container == null)
        {
            throw new InvalidOperationException("Oracle container is not running.");
        }

        var traceEndpoint = CreateOracleOpenTelemetryTraceEndpoint(collectorPort);
        if (_configuredOpenTelemetryTraceEndpoints.Contains(traceEndpoint))
        {
            return;
        }

        if (!_openTelemetryWalletConfigured)
        {
            var walletResult = await _container.ExecAsync(
                    CreateOpenTelemetryWalletConfigurationCommand(
                        Password,
                        DatabaseOpenTelemetryCertificate.RootCertificatePem,
                        DatabaseOpenTelemetryCertificate.ServerCertificatePem))
                .ConfigureAwait(false);

            if (walletResult.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to configure Oracle OpenTelemetry wallet. Exit code: {walletResult.ExitCode}. Stdout: {walletResult.Stdout} Stderr: {walletResult.Stderr}");
            }

            _openTelemetryWalletConfigured = true;
        }

        var result = await _container.ExecAsync(CreateOpenTelemetryTracingConfigurationCommand(Password, traceEndpoint, OracleOpenTelemetryTraceHost, collectorPort, _databaseOpenTelemetryCrlServer.Port)).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to enable Oracle OpenTelemetry tracing. Exit code: {result.ExitCode}. Stdout: {result.Stdout} Stderr: {result.Stderr}");
        }

        _configuredOpenTelemetryTraceEndpoints.Add(traceEndpoint);
    }

    public async Task<string> AssertOpenTelemetryTraceEndpointReachableAsync(int collectorPort)
    {
        if (_container == null)
        {
            throw new InvalidOperationException("Oracle container is not running.");
        }

        var result = await _container.ExecAsync(
                CreateOpenTelemetryEndpointConnectivityCheckCommand(
                    OracleOpenTelemetryTraceHost,
                    collectorPort,
                    _databaseOpenTelemetryCrlServer.Port,
                    DatabaseOpenTelemetryCertificate.RootCertificatePem,
                    DatabaseOpenTelemetryCertificate.ServerCertificatePem))
            .ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Oracle OpenTelemetry endpoint connectivity check failed. Exit code: {result.ExitCode}. Stdout: {result.Stdout} Stderr: {result.Stderr}");
        }

        return $"Oracle OpenTelemetry endpoint connectivity check succeeded. Stdout: {result.Stdout} Stderr: {result.Stderr}";
    }

#endif

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

        var diagnostics = await _container.ExecAsync(CreateTraceContextArchiveDiagnosticsCommand()).ConfigureAwait(false);
        throw new InvalidOperationException($"Timed out waiting for Oracle to archive propagated trace context traceid-{traceIdHex}:parentid-{parentSpanIdHex}. Diagnostic exit code: {diagnostics.ExitCode}. Stdout: {diagnostics.Stdout} Stderr: {diagnostics.Stderr}");
    }

    public async Task<string> GetOpenTelemetryServiceStatusAsync()
    {
        if (_container == null)
        {
            throw new InvalidOperationException("Oracle container is not running.");
        }

        var result = await _container.ExecAsync(CreateOpenTelemetryServiceStatusCommand(Password)).ConfigureAwait(false);
        return $"Oracle DBMS_OBSERVABILITY service status. Exit code: {result.ExitCode}. Stdout: {result.Stdout} Stderr: {result.Stderr}";
    }

    public async Task<string> GetOpenTelemetryTraceDiagnosticsAsync()
    {
        if (_container == null)
        {
            throw new InvalidOperationException("Oracle container is not running.");
        }

        var result = await _container.ExecAsync(CreateOpenTelemetryTraceDiagnosticsCommand()).ConfigureAwait(false);
        return $"Oracle OpenTelemetry trace diagnostics. Exit code: {result.ExitCode}. Stdout: {result.Stdout} Stderr: {result.Stderr}";
    }

    private static async Task ShutdownOracleContainerAsync(IContainer container)
    {
        await container.DisposeAsync().ConfigureAwait(false);
    }

    private static string[] CreateOpenTelemetryTracingConfigurationCommand(string password)
    {
        var traceEndpoint = EscapeSqlLiteral("http://127.0.0.1:4318/v1/traces");
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

    private static string[] CreateOpenTelemetryTracingConfigurationCommand(string password, string traceEndpoint, string traceHost, int tracePort, int crlPort)
    {
        var escapedTraceEndpoint = EscapeSqlLiteral(traceEndpoint);
        var escapedTraceHost = EscapeSqlLiteral(traceHost);
        var sql = @"
set define off
set serveroutput on
whenever sqlerror exit sql.sqlcode

begin
  begin
    dbms_network_acl_admin.append_host_ace(
      host => '" + escapedTraceHost + @"',
      lower_port => " + tracePort.ToString(CultureInfo.InvariantCulture) + @",
      upper_port => " + tracePort.ToString(CultureInfo.InvariantCulture) + @",
      ace => xs$ace_type(
        privilege_list => xs$name_list('connect'),
        principal_name => '" + OracleAdminUser + @"',
        principal_type => xs_acl.ptype_db));
  exception
    when others then
      if sqlcode != -24243 and instr(sqlerrm, 'already exists') = 0 then
        raise;
      end if;
  end;

  begin
    dbms_network_acl_admin.append_host_ace(
      host => '" + escapedTraceHost + @"',
      lower_port => " + crlPort.ToString(CultureInfo.InvariantCulture) + @",
      upper_port => " + crlPort.ToString(CultureInfo.InvariantCulture) + @",
      ace => xs$ace_type(
        privilege_list => xs$name_list('connect'),
        principal_name => '" + OracleAdminUser + @"',
        principal_type => xs_acl.ptype_db));
  exception
    when others then
      if sqlcode != -24243 and instr(sqlerrm, 'already exists') = 0 then
        raise;
      end if;
  end;

  begin
    dbms_network_acl_admin.append_host_ace(
      host => '" + escapedTraceHost + @"',
      ace => xs$ace_type(
        privilege_list => xs$name_list('resolve'),
        principal_name => '" + OracleAdminUser + @"',
        principal_type => xs_acl.ptype_db));
  exception
    when others then
      if sqlcode != -24243 and instr(sqlerrm, 'already exists') = 0 then
        raise;
      end if;
  end;

  dbms_observability.enable_service;
  dbms_observability.enable_service_option(option_id => dbms_observability.capture_traces);
  dbms_observability.enable_service_option(option_id => dbms_observability.export_when_empty);
  dbms_observability.add_endpoint(
    endpoint_type => dbms_observability.otel_traces,
    endpoint => '" + escapedTraceEndpoint + @"',
    credential_name => NULL);
  dbms_observability.enable_endpoint(endpoint => '" + escapedTraceEndpoint + @"');
end;
/
exit
";

        return CreateSqlPlusCommand(sql, password);
    }

    private static string[] CreateOpenTelemetryServiceStatusCommand(string password)
    {
        var sql = @"
set define off
set serveroutput on size unlimited
set feedback off pagesize 0 linesize 4000 trimspool on verify off echo off
whenever sqlerror exit sql.sqlcode

declare
  v_config varchar2(32767);
begin
  begin
    execute immediate 'select dbms_observability.show_service_status(dbms_observability.all_info) from dual' into v_config;
  exception
    when others then
      execute immediate 'select dbms_observability.show_service_status(0) from dual' into v_config;
  end;

  dbms_output.put_line(v_config);
end;
/
exit
";

        return CreateSqlPlusCommand(sql, password);
    }

#if NET
    private static string[] CreateOpenTelemetryWalletConfigurationCommand(string password, string rootCertificatePem, string serverCertificatePem)
    {
        var script = @"
set -euo pipefail

export TNS_ADMIN=" + OracleWalletDirectory + @"
if [ -z ""${JAVA_HOME:-}"" ] || [ ! -x ""${JAVA_HOME}/bin/java"" ]; then
  java_bin=""$(command -v java)""
  export JAVA_HOME=""$(dirname ""$(dirname ""$(readlink -f ""$java_bin"")"")"")""
fi
export PATH=""${JAVA_HOME}/bin:${PATH}""

cat > /tmp/otel-test-root-ca.crt <<'CERT'
" + rootCertificatePem + @"
CERT

cat > /tmp/otel-test-server.crt <<'CERT'
" + serverCertificatePem + @"
CERT

pdb_guid=""$(sqlplus -L -s " + OracleAdminUser + "/" + password + "@" + OracleDatabaseServiceName + @" <<'SQL' 2>&1 | tr -d '\r' | grep -E -m 1 '^[[:space:]]*[0-9A-Fa-f]{32}[[:space:]]*$' | tr -d '[:space:]' | tr '[:lower:]' '[:upper:]'
set heading off feedback off pagesize 0 linesize 4000 trimspool on verify off echo off
select rawtohex(guid) from v$pdbs where name = sys_context('USERENV','CON_NAME');
exit
SQL
)""
sql_pdb_guid=""$pdb_guid""

wallet_root=""$(sqlplus -L -s " + OracleAdminUser + "/" + password + "@" + OracleDatabaseServiceName + @" <<'SQL' 2>&1 | tr -d '\r' | awk 'index($0, ""/"") == 1 { gsub(/^[[:space:]]+|[[:space:]]+$/, """"); print; exit }'
set heading off feedback off pagesize 0 linesize 4000 trimspool on verify off echo off
select value from v$parameter where name = 'wallet_root';
exit
SQL
)""
if [ -z ""$wallet_root"" ]; then
  wallet_root=""${WALLET_ROOT:-/u01/app/oracle/wallets}""
fi

echo ""Using Oracle OpenTelemetry wallet root: $wallet_root""
echo ""Using Oracle OpenTelemetry PDB GUID: $pdb_guid""
existing_pdb_guid_dir=""$(find ""$wallet_root"" -maxdepth 1 -mindepth 1 -type d 2>/dev/null | awk -F/ -v guid=""$pdb_guid"" 'tolower($NF) == tolower(guid) { print $NF; exit }')""
if [ -n ""$existing_pdb_guid_dir"" ]; then
  pdb_guid=""$existing_pdb_guid_dir""
fi

configure_wallet_dir() {
  local wallet_dir=""$1""
  mkdir -p ""$wallet_dir""

  if [ ! -f ""${wallet_dir}/cwallet.sso"" ]; then
    orapki wallet create -wallet ""$wallet_dir"" -auto_login_only
  fi

  add_trusted_cert() {
    local cert=""$1""
    local output

    if output=""$(orapki wallet add -wallet ""$wallet_dir"" -trusted_cert -cert ""$cert"" -auto_login_only 2>&1)""; then
      echo ""$output""
      return 0
    fi

    if grep -q ""PKI-04003"" <<<""$output""; then
      echo ""Trusted certificate already present in $wallet_dir: $cert""
      return 0
    fi

    echo ""$output"" >&2
    return 1
  }

  add_trusted_cert /tmp/otel-test-root-ca.crt
  add_trusted_cert /tmp/otel-test-server.crt
  chmod -R u+rwX,go+rX ""$wallet_dir""
  orapki wallet display -wallet ""$wallet_dir"" -summary
}

wallet_dirs=(""${wallet_root}/${pdb_guid}/disttrc"")
pdb_guid_lower=""$(printf '%s' ""$sql_pdb_guid"" | tr '[:upper:]' '[:lower:]')""
if [ -n ""$pdb_guid_lower"" ] && [ ""$pdb_guid_lower"" != ""$pdb_guid"" ]; then
  wallet_dirs+=(""${wallet_root}/${pdb_guid_lower}/disttrc"")
fi

for wallet_dir in ""${wallet_dirs[@]}""; do
  echo ""Configuring Oracle OpenTelemetry wallet: $wallet_dir""
  configure_wallet_dir ""$wallet_dir""
done

find ""$wallet_root"" -maxdepth 3 -type f -o -type d | sort
";

        return CreateBashCommand(script);
    }

    private static string[] CreateOpenTelemetryEndpointConnectivityCheckCommand(string traceHost, int tracePort, int crlPort, string rootCertificatePem, string serverCertificatePem)
    {
        var script = @"
set -euo pipefail

trace_host='" + traceHost + @"'
trace_port='" + tracePort.ToString(CultureInfo.InvariantCulture) + @"'
crl_port='" + crlPort.ToString(CultureInfo.InvariantCulture) + @"'
crl_url=""http://${trace_host}:${crl_port}/otel-test-root-ca.crl""

cat > /tmp/otel-test-root-ca.crt <<'CERT'
" + rootCertificatePem + @"
CERT

cat > /tmp/otel-test-server.crt <<'CERT'
" + serverCertificatePem + @"
CERT

echo ""Host resolution:""
getent hosts ""$trace_host"" || true

echo ""Server certificate fields:""
openssl x509 -in /tmp/otel-test-server.crt -noout -subject -issuer -dates
openssl x509 -in /tmp/otel-test-server.crt -noout -text | grep -E -A4 'Subject Alternative Name|Extended Key Usage|Key Usage|CRL Distribution Points' || true
if ! openssl x509 -in /tmp/otel-test-server.crt -noout -text | grep -q 'CRL Distribution Points'; then
  echo ""No CRL Distribution Points extension is present in the server certificate.""
fi

echo ""CRL connectivity:""
python3 - ""$crl_url"" <<'PY'
import sys
import urllib.request
url = sys.argv[1]
with urllib.request.urlopen(url, timeout=10) as response:
    data = response.read()
open('/tmp/otel-test-root-ca.crl', 'wb').write(data)
print(f'Fetched {len(data)} byte(s) from {url}')
PY
openssl crl -in /tmp/otel-test-root-ca.crl -noout -issuer -lastupdate -nextupdate

echo ""TCP connectivity:""
timeout 5 bash -c ""cat < /dev/null > /dev/tcp/${trace_host}/${trace_port}""
echo ""TCP connect succeeded.""

echo ""TLS certificate verification:""
verify_hostname_args=""""
if openssl s_client -help 2>&1 | grep -q -- '-verify_hostname'; then
  verify_hostname_args=""-verify_hostname ${trace_host}""
fi

set +e
openssl_output=""$(timeout 10 openssl s_client -connect ""${trace_host}:${trace_port}"" -servername ""$trace_host"" -CAfile /tmp/otel-test-root-ca.crt -verify_return_error -alpn 'h2,http/1.1' $verify_hostname_args < /dev/null 2>&1)""
openssl_exit=$?
set -e
echo ""$openssl_output""
echo ""openssl s_client exit code: ${openssl_exit}""

if [ ""$openssl_exit"" -ne 124 ] && [ ""$openssl_exit"" -ne 137 ] \
   && echo ""$openssl_output"" | grep -q -- '-----BEGIN CERTIFICATE-----' \
   && echo ""$openssl_output"" | grep -Eq 'Verify return code: 0 \(ok\)|Verification: OK'; then
  exit 0
fi

exit 1
";

        return CreateBashCommand(script);
    }

    private static string CreateOracleOpenTelemetryTraceEndpoint(int collectorPort)
    {
        return $"https://{OracleOpenTelemetryTraceHost}:{collectorPort.ToString(CultureInfo.InvariantCulture)}/v1/traces";
    }

#endif

    private static string[] CreateTraceContextSearchCommand(string traceIdHex, string parentSpanIdHex)
    {
        var script = @"
trace_id='" + traceIdHex + @"'
parent_span_id='" + parentSpanIdHex + @"'
if grep -a -R -E -q ""(traceid-${trace_id}.*parentid-${parent_span_id}|traceparent:[[:space:]]*00-${trace_id}-${parent_span_id}-[0-9a-fA-F]{2})"" /u01/app/oracle/diag/rdbms/*/*/trace/*_dt*.trc 2>/dev/null; then
  exit 0
fi
exit 1
";

        return CreateBashCommand(script);
    }

    private static string[] CreateTraceContextArchiveDiagnosticsCommand()
    {
        var script = "grep -a -R -E -i 'traceparent|traceid|parentid|ora\\$opentelem' /u01/app/oracle/diag/rdbms/*/*/trace/*_dt*.trc 2>/dev/null | head -200 || true";
        return CreateBashCommand(script);
    }

    private static string[] CreateOpenTelemetryTraceDiagnosticsCommand()
    {
        var script = "find /u01/app/oracle/diag -type f \\( -name '*.trc' -o -name '*.log' \\) -print0 2>/dev/null | xargs -0 grep -a -E -i 'observab|otel|otlp|trace|export|endpoint|wallet|certificate|ssl|https|host\\.docker\\.internal|ORA-' 2>/dev/null | tail -200 || true";
        return CreateBashCommand(script);
    }

    private static async Task<IContainer> LaunchOracleContainerAsync(int port, string password, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= OracleContainerStartAttempts; attempt++)
        {
            var containersBuilder = new ContainerBuilder(OracleImage)
                .WithEnvironment("WORKLOAD_TYPE", "ATP")
                .WithEnvironment("ADMIN_PASSWORD", password)
                .WithEnvironment("WALLET_PASSWORD", password)
                .WithName($"oracle-adb-{port}-{attempt}")
                .WithPrivileged(true)
                .WithExtraHost(OracleOpenTelemetryTraceHost, "host-gateway")
                .WithResourceMapping(Encoding.UTF8.GetBytes(CreateOracleEntrypointScript()), OracleEntrypointPath, 0, 0, ExecutableFileMode)
                .WithEntrypoint(OracleEntrypointPath)
                .WithPortBinding(port, OraclePort)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(OraclePort));

            var container = containersBuilder.Build();
            try
            {
                // Pass the readiness budget token so a container whose internal port never
                // opens cancels the wait strategy instead of blocking indefinitely.
                await container.StartAsync(cancellationToken).ConfigureAwait(false);
                return container;
            }
            catch (ContainerNotRunningException)
            {
                await container.DisposeAsync().ConfigureAwait(false);
                if (attempt == OracleContainerStartAttempts)
                {
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Readiness budget exhausted while starting the container; clean up and fail fast.
                await container.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        throw new InvalidOperationException("Oracle container failed to start.");
    }

    private static string CreateOracleEntrypointScript()
    {
        return string.Join(
            "\n",
            new[]
            {
                "#!/usr/bin/env bash",
                "set -euo pipefail",
                string.Empty,
                "if [ -n \"${ORACLE_ROOT:-}\" ] && [ -f \"${ORACLE_ROOT}/POD1.zip\" ] && [ ! -f \"${ORACLE_ROOT}/.unzipped_pod1\" ]; then",
                "  unzip -o \"${ORACLE_ROOT}/POD1.zip\" -d /",
                "  touch \"${ORACLE_ROOT}/.unzipped_pod1\"",
                "  rm -rf \"${ORACLE_ROOT}/POD1.zip\"",
                "fi",
                string.Empty,
                "if [ \"${ORACLE_ENABLE_KSTRC:-true}\" = \"true\" ]; then",
                "  sqlplus / as sysdba <<'SQL'",
                "whenever sqlerror exit failure",
                "startup nomount;",
                "alter system set \"_kstrc_service_mask\"=0 scope=spfile;",
                "shutdown abort;",
                "exit;",
                "SQL",
                "fi",
                string.Empty,
                "exec /u01/scripts/entrypoint.sh",
                string.Empty,
            });
    }

    private static async Task WaitForOracleDatabaseAsync(IContainer container, string password, CancellationToken cancellationToken)
    {
        var sql = @"
set heading off feedback off pages 0
whenever sqlerror exit sql.sqlcode
select 1 from dual;
exit
";
        var command = CreateSqlPlusCommand(sql, password);

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await container.ExecAsync(command, cancellationToken).ConfigureAwait(false);
                if (result.ExitCode == 0)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Readiness budget exhausted before the database accepted SQL*Plus connections.
            throw new InvalidOperationException("Timed out waiting for Oracle ADB to accept SQLPlus connections.");
        }
    }

    private static async Task<string> CopyWalletAsync(IContainer container, int port)
    {
        var walletDirectory = Path.Combine(
            Path.GetTempPath(),
            "oracle-adb-wallets",
            $"{port.ToString(CultureInfo.InvariantCulture)}-{Guid.NewGuid():N}");
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
        return CreateBashCommand($"export TNS_ADMIN={OracleWalletDirectory}; cat <<'SQL' | sqlplus -s {OracleAdminUser}/{password}@{OracleDatabaseServiceName}\n{sql}\nSQL");
    }

    private static string[] CreateBashCommand(string script)
    {
        return ["bash", "-lc", NormalizeLineEndings(script)];
    }

    private static string NormalizeLineEndings(string value)
    {
        return ReplaceOrdinal(ReplaceOrdinal(value, "\r\n", "\n"), "\r", "\n");
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

#if NET
internal sealed class OracleDatabaseOpenTelemetryCertificate : IDisposable
{
    private const string Sha256WithRsaEncryptionOid = "1.2.840.113549.1.1.11";

    private OracleDatabaseOpenTelemetryCertificate(
        X509Certificate2 serverCertificate,
        string rootCertificatePem,
        string serverCertificatePem,
        string rootCertificateCrlPem)
    {
        ServerCertificate = serverCertificate;
        RootCertificatePem = rootCertificatePem;
        ServerCertificatePem = serverCertificatePem;
        RootCertificateCrlPem = rootCertificateCrlPem;
    }

    public X509Certificate2 ServerCertificate { get; }

    public string RootCertificatePem { get; }

    public string ServerCertificatePem { get; }

    public string RootCertificateCrlPem { get; }

    public static OracleDatabaseOpenTelemetryCertificate Create(string hostName, int crlPort)
    {
        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = DateTimeOffset.UtcNow.AddDays(30);
        var crlUrl = $"http://{hostName}:{crlPort.ToString(CultureInfo.InvariantCulture)}/otel-test-root-ca.crl";

        using var rootKey = RSA.Create(2048);
        var rootRequest = new CertificateRequest(
            "CN=otel-test-root-ca",
            rootKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: true, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
        rootRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, critical: true));
        rootRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(rootRequest.PublicKey, critical: false));

        using var rootCertificate = rootRequest.CreateSelfSigned(notBefore, notAfter);

        using var serverKey = RSA.Create(2048);
        var serverRequest = new CertificateRequest(
            $"CN={hostName}",
            serverKey,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        serverRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
        serverRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));
        var enhancedKeyUsages = new OidCollection();
        enhancedKeyUsages.Add(new Oid("1.3.6.1.5.5.7.3.1"));
        serverRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsages, critical: false));

        var subjectAlternativeNames = new SubjectAlternativeNameBuilder();
        subjectAlternativeNames.AddDnsName(hostName);
        subjectAlternativeNames.AddDnsName("localhost");
        subjectAlternativeNames.AddIpAddress(IPAddress.Loopback);
        serverRequest.CertificateExtensions.Add(subjectAlternativeNames.Build());
        serverRequest.CertificateExtensions.Add(new X509Extension("2.5.29.31", CreateCrlDistributionPointsExtension(crlUrl), critical: false));

        using var serverCertificateWithoutKey = serverRequest.Create(rootCertificate, notBefore, notAfter, Guid.NewGuid().ToByteArray());
        var serverCertificateWithEphemeralKey = serverCertificateWithoutKey.CopyWithPrivateKey(serverKey);
        var serverCertificate = PersistPrivateKeyOnWindows(serverCertificateWithEphemeralKey);
        var rootCertificateCrlPem = CreateCertificateRevocationListPem(rootCertificate, rootKey, notBefore, notAfter);

        return new OracleDatabaseOpenTelemetryCertificate(
            serverCertificate,
            rootCertificate.ExportCertificatePem(),
            serverCertificateWithoutKey.ExportCertificatePem(),
            rootCertificateCrlPem);
    }

    public void Dispose()
    {
        ServerCertificate.Dispose();
    }

    private static X509Certificate2 PersistPrivateKeyOnWindows(X509Certificate2 certificate)
    {
        if (!OperatingSystem.IsWindows())
        {
            return certificate;
        }

        using (certificate)
        {
            // Schannel cannot use the ephemeral key returned by CopyWithPrivateKey for TLS.
            var certificateBytes = certificate.Export(X509ContentType.Pkcs12);
#if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadPkcs12(
                certificateBytes,
                password: null,
                X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
#else
            return new X509Certificate2(
                certificateBytes,
                password: (string?)null,
                X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
#endif
        }
    }

    private static byte[] CreateCrlDistributionPointsExtension(string crlUrl)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.PushSequence();

        var distributionPointNameTag = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
        writer.PushSequence(distributionPointNameTag);
        writer.PushSequence(distributionPointNameTag);
        writer.WriteCharacterString(UniversalTagNumber.IA5String, crlUrl, new Asn1Tag(TagClass.ContextSpecific, 6));
        writer.PopSequence(distributionPointNameTag);
        writer.PopSequence(distributionPointNameTag);

        writer.PopSequence();
        writer.PopSequence();
        return writer.Encode();
    }

    private static string CreateCertificateRevocationListPem(X509Certificate2 issuerCertificate, RSA issuerKey, DateTimeOffset thisUpdate, DateTimeOffset nextUpdate)
    {
        var tbsWriter = new AsnWriter(AsnEncodingRules.DER);
        tbsWriter.PushSequence();
        tbsWriter.WriteInteger(1);
        WriteSha256WithRsaAlgorithmIdentifier(tbsWriter);
        tbsWriter.WriteEncodedValue(issuerCertificate.SubjectName.RawData);
        tbsWriter.WriteUtcTime(thisUpdate);
        tbsWriter.WriteUtcTime(nextUpdate);
        WriteCrlExtensions(tbsWriter);
        tbsWriter.PopSequence();

        var tbsCertificateList = tbsWriter.Encode();
        var signature = issuerKey.SignData(tbsCertificateList, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var crlWriter = new AsnWriter(AsnEncodingRules.DER);
        crlWriter.PushSequence();
        crlWriter.WriteEncodedValue(tbsCertificateList);
        WriteSha256WithRsaAlgorithmIdentifier(crlWriter);
        crlWriter.WriteBitString(signature);
        crlWriter.PopSequence();

        return PemEncode("X509 CRL", crlWriter.Encode());
    }

    private static void WriteCrlExtensions(AsnWriter tbsWriter)
    {
        var crlNumberValue = new AsnWriter(AsnEncodingRules.DER);
        crlNumberValue.WriteInteger(1);

        var extensions = new AsnWriter(AsnEncodingRules.DER);
        extensions.PushSequence();
        extensions.PushSequence();
        extensions.WriteObjectIdentifier("2.5.29.20");
        extensions.WriteOctetString(crlNumberValue.Encode());
        extensions.PopSequence();
        extensions.PopSequence();

        var crlExtensionsTag = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
        tbsWriter.PushSequence(crlExtensionsTag);
        tbsWriter.WriteEncodedValue(extensions.Encode());
        tbsWriter.PopSequence(crlExtensionsTag);
    }

    private static void WriteSha256WithRsaAlgorithmIdentifier(AsnWriter writer)
    {
        writer.PushSequence();
        writer.WriteObjectIdentifier(Sha256WithRsaEncryptionOid);
        writer.WriteNull();
        writer.PopSequence();
    }

    private static string PemEncode(string label, byte[] bytes)
    {
        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"-----BEGIN {label}-----\n");
        var base64 = Convert.ToBase64String(bytes);
        for (var i = 0; i < base64.Length; i += 64)
        {
            builder.Append(base64, i, Math.Min(64, base64.Length - i));
            builder.Append('\n');
        }

        builder.Append(CultureInfo.InvariantCulture, $"-----END {label}-----\n");
        return builder.ToString();
    }
}

internal sealed class OracleDatabaseOpenTelemetryCrlServer : IDisposable
{
    private const string CrlPath = "/otel-test-root-ca.crl";

    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _acceptLoop;
    private byte[] _crlPem = [];

    public OracleDatabaseOpenTelemetryCrlServer()
    {
        _listener = new TcpListener(IPAddress.IPv6Any, 0);
        _listener.Server.DualMode = true;
        _listener.Start();

        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptLoop = Task.Run(AcceptLoopAsync);
    }

    public int Port { get; }

    public void SetCrlPem(string crlPem)
    {
        _crlPem = Encoding.ASCII.GetBytes(crlPem);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _listener.Dispose();

        try
        {
            _acceptLoop.GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            // The listener may already be stopped while the accept loop is exiting.
        }

        _cts.Dispose();
    }

    private static async Task<string> ReadHeadersAsync(Stream stream)
    {
        using var buffer = new MemoryStream();
        var current = new byte[1];
        while (buffer.Length < 32 * 1024)
        {
            var read = await stream.ReadAsync(current).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            buffer.WriteByte(current[0]);
            var bytes = buffer.GetBuffer();
            var length = (int)buffer.Length;
            if (length >= 4 &&
                bytes[length - 4] == '\r' &&
                bytes[length - 3] == '\n' &&
                bytes[length - 2] == '\r' &&
                bytes[length - 1] == '\n')
            {
                return Encoding.ASCII.GetString(bytes, 0, length);
            }
        }

        return string.Empty;
    }

    private async Task AcceptLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(client), _cts.Token);
            }
            catch (ObjectDisposedException)
            {
                client?.Dispose();
                return;
            }
            catch (SocketException)
            {
                client?.Dispose();
                if (_cts.IsCancellationRequested)
                {
                    return;
                }

                throw;
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var headers = await ReadHeadersAsync(stream).ConfigureAwait(false);
                var requestLine = headers.Split(["\r\n"], StringSplitOptions.None).FirstOrDefault() ?? string.Empty;
                var statusCode = requestLine.StartsWith("GET " + CrlPath + " ", StringComparison.Ordinal) ? 200 : 404;
                var body = statusCode == 200 ? _crlPem : Encoding.ASCII.GetBytes("Not found");
                var contentType = statusCode == 200 ? "application/pkix-crl" : "text/plain";
                var responseHeaders = Encoding.ASCII.GetBytes(
                    $"HTTP/1.1 {statusCode.ToString(CultureInfo.InvariantCulture)} {(statusCode == 200 ? "OK" : "Not Found")}\r\n" +
                    $"Content-Type: {contentType}\r\n" +
                    $"Content-Length: {body.Length.ToString(CultureInfo.InvariantCulture)}\r\n" +
                    "Connection: close\r\n\r\n");
                await stream.WriteAsync(responseHeaders).ConfigureAwait(false);
                await stream.WriteAsync(body).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or SocketException)
        {
            // Best effort CRL responder for the Oracle integration test.
        }
    }
}
#endif

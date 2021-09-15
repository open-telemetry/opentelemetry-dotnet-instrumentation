# allow overriding defaults using environment variables
if (Test-Path env:SvcFabDir) { $SvcFabDir = $env:SvcFabDir } else { $SvcFabDir = 'D:\SvcFab' }
if (Test-Path env:OTEL_TRACER_VERSION) { $OTEL_TRACER_VERSION = $env:OTEL_TRACER_VERSION } else { $OTEL_TRACER_VERSION = '0.0.1' }
if (Test-Path env:OTEL_TRACER_URL) { $OTEL_TRACER_URL = $env:OTEL_TRACER_URL } else { $OTEL_TRACER_URL = "https://github.com/DataDog/dd-trace-dotnet/releases/download/v$OTEL_TRACER_VERSION/windows-tracer-home.zip" }
if (Test-Path env:OTEL_DOTNET_TRACER_HOME) { $OTEL_DOTNET_TRACER_HOME = $env:OTEL_DOTNET_TRACER_HOME } else { $OTEL_DOTNET_TRACER_HOME = "$SvcFabDir\datadog-dotnet-tracer\v$OTEL_TRACER_VERSION" }

Write-Host "[DatadogInstall.ps1] Installing Datadog .NET Tracer v$OTEL_TRACER_VERSION"

# download, extract, and delete the archive
$ArchivePath = "$SvcFabDir\windows-tracer-home.zip"
Write-Host "[DatadogInstall.ps1] Downloading $OTEL_TRACER_URL to $ArchivePath"
Invoke-WebRequest $OTEL_TRACER_URL -OutFile $ArchivePath

Write-Host "[DatadogInstall.ps1] Extracting to $OTEL_DOTNET_TRACER_HOME"
Expand-Archive -Force -Path "$SvcFabDir\windows-tracer-home.zip" -DestinationPath $OTEL_DOTNET_TRACER_HOME

Write-Host "[DatadogInstall.ps1] Deleting $ArchivePath"
Remove-Item $ArchivePath

# create a folder for log files
$LOGS_PATH = "$SvcFabDir\datadog-dotnet-tracer-logs"

if (-not (Test-Path -Path $LOGS_PATH -PathType Container)) {
  Write-Host "[DatadogInstall.ps1] Creating logs folder $LOGS_PATH"
  New-Item -ItemType Directory -Force -Path $LOGS_PATH
}

function Set-MachineEnvironmentVariable {
    param(
      [string]$name,
      [string]$value
    )

    Write-Host "[DatadogInstall.ps1] Setting environment variable $name=$value"
    [System.Environment]::SetEnvironmentVariable($name, $value, [System.EnvironmentVariableTarget]::Machine)
}

Set-MachineEnvironmentVariable 'OTEL_DOTNET_TRACER_HOME' $OTEL_DOTNET_TRACER_HOME
Set-MachineEnvironmentVariable 'OTEL_INTEGRATIONS' "$OTEL_DOTNET_TRACER_HOME\integrations.json"
Set-MachineEnvironmentVariable 'OTEL_TRACE_LOG_DIRECTORY' "$LOGS_PATH"

# Set-MachineEnvironmentVariable'COR_ENABLE_PROFILING' '0' # Enable per app
Set-MachineEnvironmentVariable 'COR_PROFILER' '{918728DD-259F-4A6A-AC2B-B85E1B658318}'
Set-MachineEnvironmentVariable 'COR_PROFILER_PATH_32' "$OTEL_DOTNET_TRACER_HOME\win-x86\OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.dll"
Set-MachineEnvironmentVariable 'COR_PROFILER_PATH_64' "$OTEL_DOTNET_TRACER_HOME\win-x64\OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.dll"

# Set-MachineEnvironmentVariable 'CORECLR_ENABLE_PROFILING' '0' # Enable per app
Set-MachineEnvironmentVariable 'CORECLR_PROFILER' '{918728DD-259F-4A6A-AC2B-B85E1B658318}'
Set-MachineEnvironmentVariable 'CORECLR_PROFILER_PATH_32' "$OTEL_DOTNET_TRACER_HOME\win-x86\OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.dll"
Set-MachineEnvironmentVariable 'CORECLR_PROFILER_PATH_64' "$OTEL_DOTNET_TRACER_HOME\win-x64\OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.dll"

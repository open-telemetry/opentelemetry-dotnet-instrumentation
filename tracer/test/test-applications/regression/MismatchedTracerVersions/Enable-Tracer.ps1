#Requires -Version 7

Param (
    [Parameter(Mandatory)]
    [ValidateScript( { Test-Path $_ -PathType 'Container' })]
    [String]
    $TracerHome,

    [String]
    $LogPath
)

$ErrorActionPreference = 'Stop'

$TracerGuid = '{918728DD-259F-4A6A-AC2B-B85E1B658318}'
$TracerPath32 = ''
$TracerPath64 = ''
$TracerHome = Resolve-Path $TracerHome
$JsonPath = Join-Path $TracerHome 'integrations.json' -Resolve

if ($LogPath -eq "") {
    $LogPath = Join-Path $TracerHome 'logs'
}

New-Item -Path $LogPath -ItemType Directory -Force | Out-Null
$LogPath = Resolve-Path $LogPath

if ($IsWindows) {
    Write-Host 'Setting environment variables for Windows'
    $TracerPath32 = Join-Path $TracerHome 'win-x86\OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.dll' -Resolve
    $TracerPath64 = Join-Path $TracerHome 'win-x64\OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.dll' -Resolve
}

if ($IsLinux) {
    Write-Host 'Setting environment variables for Linux'
    $TracerPath64 = Join-Path $TracerHome 'OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.so' -Resolve
}

if ($IsMacOS) {
    Write-Host 'Setting environment variables for macOS'
    $TracerPath64 = Join-Path $TracerHome 'OpenTelemetry.AutoInstrumentation.ClrProfiler.Native.dylib' -Resolve
}

function Set-EnvironmentVariable {
    param([String] $name, [String] $value)

    Write-Verbose "$name=$value"
    Set-Item -Path "Env:$name" -Value $value
}

if ($IsWindows) {
    Set-EnvironmentVariable 'OTEL_PROFILER_EXCLUDE_PROCESSES' 'dotnet.exe;devenv.exe;Microsoft.ServiceHub.Controller.exe;ServiceHub.Host.CLR.exe;ServiceHub.TestWindowStoreHost.exe;ServiceHub.DataWarehouseHost.exe;sqlservr.exe;VBCSCompiler.exe;iisexpresstray.exe;msvsmon.exe;PerfWatson2.exe;ServiceHub.IdentityHost.exe;ServiceHub.VSDetouredHost.exe;ServiceHub.SettingsHost.exe;ServiceHub.Host.CLR.x86.exe;vstest.console.exe;ServiceHub.RoslynCodeAnalysisService32.exe;testhost.x86.exe;MSBuild.exe;ServiceHub.ThreadedWaitDialog.exe;OmniSharp.exe;CodeHelper.exe;pwsh.exe'
} else {
    Set-EnvironmentVariable 'OTEL_PROFILER_EXCLUDE_PROCESSES' 'dotnet'
}

Set-EnvironmentVariable 'OTEL_DOTNET_TRACER_HOME' $TracerHome
Set-EnvironmentVariable 'OTEL_INTEGRATIONS' $JsonPath
Set-EnvironmentVariable 'OTEL_TRACE_LOG_DIRECTORY' $LogPath

Set-EnvironmentVariable 'CORECLR_ENABLE_PROFILING' '1'
Set-EnvironmentVariable 'CORECLR_PROFILER' $TracerGuid
Set-EnvironmentVariable 'CORECLR_PROFILER_PATH_32' $TracerPath32
Set-EnvironmentVariable 'CORECLR_PROFILER_PATH_64' $TracerPath64

Set-EnvironmentVariable 'COR_ENABLE_PROFILING' '1'
Set-EnvironmentVariable 'COR_PROFILER' $TracerGuid
Set-EnvironmentVariable 'COR_PROFILER_PATH_32' $TracerPath32
Set-EnvironmentVariable 'COR_PROFILER_PATH_64' $TracerPath64

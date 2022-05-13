# Instrument a Windows Service running a .NET application

We recommend setting environment variables for a Windows Service
using the Windows Registry.

> Remember to restart the Windows Service
  after making changes to its configuration.

## Set environment variables for a Windows Service

The registry key of a given Windows Service (named `$svcName`) is located under:

```powershell
HKLM\SYSTEM\CurrentControlSet\Services\$svcName
```

The environment variables can be defined
in a `REG_MULTI_SZ` (multiline registry value) called `Environment`
in the following format:

```env
Var1=Value1
Var2=Value2
```

Below is an example how instrumentation can be configured in PowerShell:

```powershell
$installationLocation = "C:\some\dir" # The path where you put the OTel .NET AutoInstrumentation binaries
$svcName = "MySrv"    # The name of the Windows Service
[string[]] $vars = @(
   "COR_ENABLE_PROFILING=1",
   "COR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}",
   "COR_PROFILER_PATH_64=$installationLocation\win-x64\OpenTelemetry.AutoInstrumentation.Native.dll",
   "COR_PROFILER_PATH_32=$installationLocation\win-x86\OpenTelemetry.AutoInstrumentation.Native.dll",
   "CORECLR_ENABLE_PROFILING=1",
   "CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}",
   "CORECLR_PROFILER_PATH_64=$installationLocation\win-x64\OpenTelemetry.AutoInstrumentation.Native.dll",
   "CORECLR_PROFILER_PATH_32=$installationLocation\win-x86\OpenTelemetry.AutoInstrumentation.Native.dll",
   "DOTNET_ADDITIONAL_DEPS=$installationLocation\AdditionalDeps",
   "DOTNET_SHARED_STORE=$installationLocation\store",
   "DOTNET_STARTUP_HOOKS=$installationLocation\netcoreapp3.1\OpenTelemetry.AutoInstrumentation.StartupHook.dll",
   "OTEL_DOTNET_AUTO_HOME=$installationLocation",
   "OTEL_DOTNET_AUTO_INTEGRATIONS_FILE=$installationLocation\integrations.json",
   "OTEL_SERVICE_NAME=my-service-name"
)
Set-ItemProperty HKLM:SYSTEM\CurrentControlSet\Services\$svcName -Name Environment -Value $vars
```

# Instrument a Windows Service running a .NET application

## Setup

Use the `OpenTelemetry.DotNet.Auto.psm1` PowerShell module
to set up automatic instrumentation for a Windows Service:

```powershell
# Import the module
Import-Module "OpenTelemetry.DotNet.Auto.psm1"

# Install core files
Install-OpenTelemetryCore

# Set up your Windows Service instrumentation
Register-OpenTelemetryForWindowsService -WindowsServiceName "WindowsServiceName" -OTelServiceName "MyServiceDisplayName"
```

> [!WARNING]
> `Register-OpenTelemetryForWindowsService` performs a service restart by default.
> Use `-NoReset` to skip the restart.

## Configuration

> [!NOTE]
> Remember to restart the Windows Service after making configuration changes.
> You can do it by executing
> `Restart-Service -Name $WindowsServiceName -Force` in PowerShell.

For .NET Framework applications you can configure the most common `OTEL_` settings
(like `OTEL_RESOURCE_ATTRIBUTES`) via `appSettings` in `App.config`.

The alternative is to set environment variables for the Windows Service
in the Windows Registry.

The registry key of a given Windows Service (named `$svcName`) is located under:

```powershell
HKLM\SYSTEM\CurrentControlSet\Services\$svcName
```

The environment variables are defined
in a `REG_MULTI_SZ` (multiline registry value) called `Environment`
in the following format:

```env
Var1=Value1
Var2=Value2
```

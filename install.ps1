#
# Copyright The OpenTelemetry Authors
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#

#Requires -RunAsAdministrator

<#
 .SYNOPSIS
    Installs OpenTelemetry .NET Automatic Instrumentation 
 .PARAMETER InstallDir
    Default: <auto> - the default value is AppData
    Install path of the OpenTelemetry .NET Automatic Instrumentation
    Possible values: <auto>, ProgramFiles, AppData, (Custom path)
#>
[cmdletbinding()]
param(
    [string]$InstallDir="<auto>"
)

function Get-Install-Directory([string]$InstallDir) {
    $dir = "OpenTelemetry .NET AutoInstrumentation"
    
    if($InstallDir -eq "<auto>" -or $installDir -eq "AppData") {
        return (Join-Path $env:LOCALAPPDATA "Programs" | Join-Path -ChildPath $dir)
    }
    elseif($InstallDir -eq "ProgramFiles"){
        return (Join-Path "C:\Program Files\" $dir)
    } 
    elseif(Test-Path $InstallDir -IsValid) {
        return $InstallDir
    }

    throw "Invalid install directory provided '$InstallDir'"
}

function Get-Temp-Directory() {
    return $env:temp
}

function Prepare-Install-Directory([string]$InstallDir) {
    New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
}

function Download-OpenTelemetry([string]$Version, [string]$Path) {
    $archive = "opentelemetry-dotnet-instrumentation-windows.zip"
    $dlUrl = "https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/$Version/$archive"
    $dlPath = Join-Path $Path $archive

    Invoke-WebRequest -Uri $dlUrl -OutFile $dlPath

    return $dlPath
}

function Extract-OpenTelemetry([string]$DlPath, [string] $InstallPath) {
    Expand-Archive $DlPath $InstallPath -Force
}

function Setup-OpenTelemetry-Environment([string] $InstallPath) {
    $target = [System.EnvironmentVariableTarget]::Machine

    # .NET Framework
    [System.Environment]::SetEnvironmentVariable('COR_PROFILER','{918728DD-259F-4A6A-AC2B-B85E1B658318}', $target)
    [System.Environment]::SetEnvironmentVariable('COR_PROFILER_PATH_32', (Join-Path $InstallPath "/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll"), $target)
    [System.Environment]::SetEnvironmentVariable('COR_PROFILER_PATH_64', (Join-Path $InstallPath "/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll"), $target)

    # .NET Core
    [System.Environment]::SetEnvironmentVariable('CORECLR_PROFILER','{918728DD-259F-4A6A-AC2B-B85E1B658318}', $target)
    [System.Environment]::SetEnvironmentVariable('CORECLR_PROFILER_PATH_32', (Join-Path $InstallPath "/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll"), $target)
    [System.Environment]::SetEnvironmentVariable('CORECLR_PROFILER_PATH_64', (Join-Path $InstallPath "/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll"), $target)

    # .NET Common
    [System.Environment]::SetEnvironmentVariable('DOTNET_ADDITIONAL_DEPS', (Join-Path $InstallPath "AdditionalDeps"), $target)
    [System.Environment]::SetEnvironmentVariable('DOTNET_SHARED_STORE', (Join-Path $InstallPath "store"), $target)
    [System.Environment]::SetEnvironmentVariable('DOTNET_STARTUP_HOOKS', (Join-Path $InstallPath "netcoreapp3.1/OpenTelemetry.AutoInstrumentation.StartupHook.dll"), $target)

    # ASP.NET Core
    [System.Environment]::SetEnvironmentVariable('ASPNETCORE_HOSTINGSTARTUPASSEMBLIES', "OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper", $target)

    # OpenTelemetry
    [System.Environment]::SetEnvironmentVariable('OTEL_DOTNET_AUTO_HOME', $InstallPath, $target)
    [System.Environment]::SetEnvironmentVariable('OTEL_DOTNET_AUTO_INTEGRATIONS_FILE', (Join-Path $InstallPath "integrations.json"), $target)
}

function Setup-IIS-Environment() {
    Setup-Windows-Service -ServiceName "W3SVC"
    Setup-Windows-Service -ServiceName "WAS"
}

function Setup-Windows-Service([string]$ServiceName) {
    [string[]] $vars = @(
       "COR_ENABLE_PROFILING=1",
       "CORECLR_ENABLE_PROFILING=1"
    )

    $regPath = "HKLM:SYSTEM\CurrentControlSet\Services\"
    $regKey = Join-Path $regPath $ServiceName
    
    if(Test-Path $regKey) {
        Set-ItemProperty $regKey -Name Environment -Value $vars
    } else {
        throw "Invalid service '$ServiceName'. Service does not exist."
    }
}

$Version = "v0.3.1-beta.1"
$SetupPath = Get-Install-Directory $InstallDir
$TempDir = Get-Temp-Directory
$DlPath = $null

try {
    $DlPath = Download-OpenTelemetry $Version $TempDir
    Prepare-Install-Directory $SetupPath

    Extract-OpenTelemetry $DlPath $SetupPath
    Setup-OpenTelemetry-Environment $SetupPath
    Setup-IIS-Environment
} 
catch {
    $message = $_
    Write-Error "Could not setup OpenTelemetry .NET Automatic Instrumentation! $message"
} 
finally {
    if($DlPath -ne $null) {
        # Cleanup
        Remove-Item $DlPath
    }
}
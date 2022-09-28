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

<#
 .SYNOPSIS
    Installs OpenTelemetry .NET Automatic Instrumentation 
 .PARAMETER OS
    Default: windows
    Selects distribution based on operating system. 
    Possible values: windows, linux, macos
 .PARAMETER Architecture
    Default: <auto> - this value represents currently running OS architecture
    Architecture of CLR profiler binaries to be installed.
    Possible values: <auto>, x64, x86
#>
[cmdletbinding()]
param(
    [string]$OS="windows",
    [string]$Architecture="<auto>"
)

function Get-Machine-Architecture() {
    # On PS x86, PROCESSOR_ARCHITECTURE reports x86 even on x64 systems.
    # To get the correct architecture, we need to use PROCESSOR_ARCHITEW6432.
    # PS x64 doesn't define this, so we fall back to PROCESSOR_ARCHITECTURE.
    # Possible values: amd64, x64, x86, arm64, arm
    if( $ENV:PROCESSOR_ARCHITEW6432 -ne $null ) {
        return $ENV:PROCESSOR_ARCHITEW6432
    }

    try {        
        if( ((Get-CimInstance -ClassName CIM_OperatingSystem).OSArchitecture) -like "ARM*") {
            if( [Environment]::Is64BitOperatingSystem )
            {
                return "arm64"
            }  
            return "arm"
        }
    }
    catch {
        # Machine doesn't support Get-CimInstance
    }

    return $ENV:PROCESSOR_ARCHITECTURE
}

function Get-CLIArchitecture-From-Architecture([string]$Architecture) {
    if ($Architecture -eq "<auto>") {
        $Architecture = Get-Machine-Architecture
    }

    switch ($Architecture.ToLowerInvariant()) {
        { ($_ -eq "amd64") -or ($_ -eq "x64") } { return "x64" }
        { $_ -eq "x86" } { return "x86" }
        default { throw "Architecture '$Architecture' not supported." }
    }
}

function Get-Install-Directory([string]$OS, [string]$Architecture) {
    $dir = "OpenTelemetry .NET AutoInstrumentation"

    if($OS -eq "windows"){
        if($Architecture -eq "x86"){
            return (Join-Path "C:\Program Files (x86)\" $dir)
        } elseif($Architecture -eq "x64") {
            return (Join-Path "C:\Program Files\" $dir)
        }
    }

    throw "TODO: Not supported yet."
}

function Get-Temp-Directory([string]$OS) {
    if($OS -eq "windows") {
        return $env:temp
    }

    throw "TODO: Not supported yet."
}

function Prepare-Install-Directory([string]$InstallDir) {
    New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
}

function Download-OpenTelemetry([string]$OS, [string]$Architecture, [string]$Version, [string]$Path) {
    $archive = $null

    if($OS -eq "windows") {
        $archive = "opentelemetry-dotnet-instrumentation-windows.zip"
    } else {
        throw "TODO: Not supported yet."
    }

    $dlUrl = "https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/download/$Version/$archive"
    $dlPath = Join-Path $Path $archive
    Invoke-WebRequest -Uri $dlUrl -OutFile $dlPath

    return $dlPath
}

function Extract-OpenTelemetry([string]$DlPath, [string] $InstallPath) {
    Expand-Archive $DlPath $InstallPath -Force
}

function Setup-OpenTelemetry-Environment([string] $OS, [string] $InstallPath) {
    $target = [System.EnvironmentVariableTarget]::Machine

    if($OS -eq "windows") {
        # .NET Framework
        [System.Environment]::SetEnvironmentVariable('COR_PROFILER','{918728DD-259F-4A6A-AC2B-B85E1B658318}', $target)
        [System.Environment]::SetEnvironmentVariable('COR_PROFILER_PATH_32', (Join-Path $InstallPath "/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll"), $target)
        [System.Environment]::SetEnvironmentVariable('COR_PROFILER_PATH_64', (Join-Path $InstallPath "/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll"), $target)

        # .NET Core
        [System.Environment]::SetEnvironmentVariable('CORECLR_PROFILER','{918728DD-259F-4A6A-AC2B-B85E1B658318}', $target)
        [System.Environment]::SetEnvironmentVariable('CORECLR_PROFILER_PATH_32', (Join-Path $InstallPath "/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll"), $target)
        [System.Environment]::SetEnvironmentVariable('CORECLR_PROFILER_PATH_64', (Join-Path $InstallPath "/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll"), $target)
    }

    # .NET Common
    [System.Environment]::SetEnvironmentVariable('DOTNET_ADDITIONAL_DEPS', (Join-Path $InstallPath "AdditionalDeps", $target))
    [System.Environment]::SetEnvironmentVariable('DOTNET_SHARED_STORE', (Join-Path $InstallPath "store", $target))
    [System.Environment]::SetEnvironmentVariable('DOTNET_STARTUP_HOOKS', (Join-Path $InstallPath "netcoreapp3.1/OpenTelemetry.AutoInstrumentation.StartupHook.dll"), $target)

    # ASP.NET Core
    [System.Environment]::SetEnvironmentVariable('ASPNETCORE_HOSTINGSTARTUPASSEMBLIES', "OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper", $target)

    # OpenTelemetry
    [System.Environment]::SetEnvironmentVariable('OTEL_DOTNET_AUTO_HOME', $InstallPath, $target)
    [System.Environment]::SetEnvironmentVariable('OTEL_DOTNET_AUTO_INTEGRATIONS_FILE', (Join-Path $InstallPath "integrations.json"), $target)
}

$Version = "v0.3.1-beta.1"
$CLIArchitecture = Get-CLIArchitecture-From-Architecture $Architecture
$InstallDir = Get-Install-Directory $OS $CLIArchitecture
$TempDir = Get-Temp-Directory $OS

Write-Output $OS
Write-Output $CLIArchitecture
Write-Output $InstallDir
Write-Output $TempDir

$DlPath = Download-OpenTelemetry $OS $Architecture $Version $TempDir
Prepare-Install-Directory $InstallDir

Extract-OpenTelemetry $DlPath $InstallDir
Setup-OpenTelemetry-Environment $OS $InstallDir

# Cleanup
Remove-Item $DlPath
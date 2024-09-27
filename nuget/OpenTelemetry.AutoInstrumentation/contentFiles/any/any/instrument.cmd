@echo off
setlocal ENABLEDELAYEDEXPANSION

:: This script is expected to be used in a build that specified a RuntimeIdentifier (RID)
set BASE_PATH=%~dp0
set COR_PROFILER_PATH=%BASE_PATH%OpenTelemetry.AutoInstrumentation.Native.dll

:: Validate
IF EXIST %COR_PROFILER_PATH% (
    set CORECLR_PROFILER_PATH=!COR_PROFILER_PATH!
) ELSE (
    set "COR_PROFILER_PATH="

    echo Unable to locate the native profiler inside current directory, possibly due to runtime identifier not being specified when building/publishing. ^
Attempting to use the native profiler from runtimes\win-x64\native and runtimes\win-x86\native subdirectories.

    set COR_PROFILER_PATH_64=%BASE_PATH%runtimes\win-x64\native\OpenTelemetry.AutoInstrumentation.Native.dll
    set COR_PROFILER_PATH_32=%BASE_PATH%runtimes\win-x86\native\OpenTelemetry.AutoInstrumentation.Native.dll

    IF EXIST !COR_PROFILER_PATH_64! (
        IF EXIST !COR_PROFILER_PATH_32! (
            set CORECLR_PROFILER_PATH_32=!COR_PROFILER_PATH_32!
        ) ELSE (
            set "COR_PROFILER_PATH_32="
        )
        set CORECLR_PROFILER_PATH_64=!COR_PROFILER_PATH_64!
    ) ELSE (
        set "COR_PROFILER_PATH_64="
        IF EXIST !COR_PROFILER_PATH_32! (
            set CORECLR_PROFILER_PATH_32=!COR_PROFILER_PATH_32!
        ) ELSE (
            set "COR_PROFILER_PATH_32="
            echo Unable to locate the native profiler. 1>&2
            exit /b 1
        )
    )
)

:: Settings for .NET Framework
set COR_ENABLE_PROFILING=1
set COR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}

:: On .NET Framework automatic assembly redirection MUST be disabled. This setting
:: is ignored on .NET. This is necessary because the NuGet package doesn't bring
:: the pre-defined versions of the transitive dependencies used in the automatic
:: redirection. Instead the transitive dependencies versions are determined by
:: the NuGet version resolution algorithm when building the application.
set OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED=false

:: Settings for .NET
set ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper
set CORECLR_ENABLE_PROFILING=1
set CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
set DOTNET_STARTUP_HOOKS=%BASE_PATH%OpenTelemetry.AutoInstrumentation.StartupHook.dll

:: Settings for OpenTelemetry
set OTEL_DOTNET_AUTO_HOME=%BASE_PATH%
set OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED=false

@echo on
%*

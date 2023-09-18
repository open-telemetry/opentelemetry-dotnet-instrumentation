@echo off
setlocal

:: This script is expected to be used in a build that specified a RuntimeIdentifier (RID)
set BASE_PATH=%~dp0

:: Settings for .NET Framework
set COR_ENABLE_PROFILING=1
set COR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
set COR_PROFILER_PATH=%BASE_PATH%OpenTelemetry.AutoInstrumentation.Native.dll

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
set CORECLR_PROFILER_PATH=%BASE_PATH%OpenTelemetry.AutoInstrumentation.Native.dll
set DOTNET_STARTUP_HOOKS=%BASE_PATH%OpenTelemetry.AutoInstrumentation.StartupHook.dll

:: Settings for OpenTelemetry
set OTEL_DOTNET_AUTO_HOME=%BASE_PATH%
set OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED=false

@echo on
%*

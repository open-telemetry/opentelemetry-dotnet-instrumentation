REM SET SOLUTION_DIR=C:\Github\otel-trace-dotnet

SET TOOL_BUILD_CONFIG=Release
SET INTEGRATIONS_PROJ=%SOLUTION_DIR%\src\Datadog.Trace.ClrProfiler.Managed\Datadog.Trace.ClrProfiler.Managed.csproj
SET OUTPUT_DIR=%SOLUTION_DIR%\build\tools\PrepareRelease\bin\tracer-home

RMDIR "%OUTPUT_DIR%" /S /Q
dotnet msbuild "%SOLUTION_DIR%\Datadog.Trace.proj" /t:PublishManagedProfilerOnDisk /p:Configuration=Release;TracerHomeDirectory=%OUTPUT_DIR%

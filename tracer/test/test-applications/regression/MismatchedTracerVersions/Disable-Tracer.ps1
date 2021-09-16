$ErrorActionPreference = 'SilentlyContinue'

Remove-Item 'env:OTEL_DOTNET_TRACER_HOME'
Remove-Item 'env:OTEL_INTEGRATIONS'
Remove-Item 'env:OTEL_TRACE_LOG_DIRECTORY'
Remove-Item 'env:OTEL_PROFILER_EXCLUDE_PROCESSES'

Remove-Item 'env:CORECLR_ENABLE_PROFILING'
Remove-Item 'env:CORECLR_PROFILER'
Remove-Item 'env:CORECLR_PROFILER_PATH_32'
Remove-Item 'env:CORECLR_PROFILER_PATH_64'

Remove-Item 'env:COR_ENABLE_PROFILING'
Remove-Item 'env:COR_PROFILER'
Remove-Item 'env:COR_PROFILER_PATH_32'
Remove-Item 'env:COR_PROFILER_PATH_64'

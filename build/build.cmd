IF "%buildConfiguration%"=="" (SET configuration=Debug) ELSE (SET configuration=%buildConfiguration%)

nuget restore Datadog.Trace.sln
nuget restore src\OpenTelemetry.DynamicActivityBinding\OpenTelemetry.DynamicActivityBinding.sln
msbuild Datadog.Trace.proj /t:BuildCsharp /p:Configuration=%configuration%
dotnet pack -c %configuration% -o src\bin\SignalFx.Tracing src\Datadog.Trace\Datadog.Trace.csproj
dotnet pack -c %configuration% -o src\bin\SignalFx.Tracing.OpenTracing src\Datadog.Trace.OpenTracing\Datadog.Trace.OpenTracing.csproj
msbuild Datadog.Trace.proj /t:BuildCpp /p:Configuration=%configuration%;Platform=x64
msbuild Datadog.Trace.proj /t:BuildCpp /p:Configuration=%configuration%;Platform=x86
msbuild Datadog.Trace.proj /t:msi /p:Configuration=%configuration%;Platform=x64
msbuild Datadog.Trace.proj /t:msi /p:Configuration=%configuration%;Platform=x86
msbuild Datadog.Trace.proj /t:CreateHomeDirectory /p:Configuration=%configuration%;Platform=x64
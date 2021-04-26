rem To run this script locally you need the following tools to be added the path: nuget, msbuild, dotnet. 

setlocal

IF "%buildConfiguration%"=="" (SET configuration=Debug) ELSE (SET configuration=%buildConfiguration%)

nuget restore Datadog.Trace.sln
if %errorlevel% neq 0 exit /b %errorlevel%
nuget restore src\OpenTelemetry.DynamicActivityBinding\OpenTelemetry.DynamicActivityBinding.sln
if %errorlevel% neq 0 exit /b %errorlevel%
msbuild Datadog.Trace.proj /t:BuildCsharp /p:Configuration=%configuration%
if %errorlevel% neq 0 exit /b %errorlevel%
msbuild Datadog.Trace.proj /t:BuildCpp /p:Configuration=%configuration%;Platform=x64
if %errorlevel% neq 0 exit /b %errorlevel%
msbuild Datadog.Trace.proj /t:BuildCpp /p:Configuration=%configuration%;Platform=x86
if %errorlevel% neq 0 exit /b %errorlevel%
msbuild Datadog.Trace.proj /t:msi /p:Configuration=%configuration%;Platform=x64
if %errorlevel% neq 0 exit /b %errorlevel%
msbuild Datadog.Trace.proj /t:msi /p:Configuration=%configuration%;Platform=x86
if %errorlevel% neq 0 exit /b %errorlevel%
msbuild Datadog.Trace.proj /t:CreateHomeDirectory /p:Configuration=%configuration%;Platform=x64
if %errorlevel% neq 0 exit /b %errorlevel%
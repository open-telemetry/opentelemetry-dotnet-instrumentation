#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

cd "$DIR/../.."

PUBLISH_OUTPUT="$( pwd )/src/bin/managed-publish"
mkdir -p "$PUBLISH_OUTPUT/netstandard2.0"
mkdir -p "$PUBLISH_OUTPUT/netcoreapp3.1"

dotnet build -c $buildConfiguration src/Datadog.Trace.ClrProfiler.Managed.Loader/Datadog.Trace.ClrProfiler.Managed.Loader.csproj

for proj in Datadog.Trace Datadog.Trace.OpenTracing ; do
    dotnet publish -f netstandard2.0 -c $buildConfiguration src/$proj/$proj.csproj
    dotnet publish -f netcoreapp3.1 -c $buildConfiguration src/$proj/$proj.csproj
done

dotnet publish -f netstandard2.0 -c $buildConfiguration src/Datadog.Trace.ClrProfiler.Managed/Datadog.Trace.ClrProfiler.Managed.csproj -o "$PUBLISH_OUTPUT/netstandard2.0"
dotnet publish -f netcoreapp3.1 -c $buildConfiguration src/Datadog.Trace.ClrProfiler.Managed/Datadog.Trace.ClrProfiler.Managed.csproj -o "$PUBLISH_OUTPUT/netcoreapp3.1"

# Only build Samples.AspNetCoreMvc21 for netcoreapp2.1
if [ "$publishTargetFramework" == "netcoreapp2.1" ]
then
    dotnet publish -f $publishTargetFramework -c $buildConfiguration test/test-applications/integrations/Samples.AspNetCoreMvc21/Samples.AspNetCoreMvc21.csproj -p:Configuration=$buildConfiguration -p:ManagedProfilerOutputDirectory="$PUBLISH_OUTPUT"
fi

# Only build Samples.AspNetCoreMvc30 for netcoreapp3.0
if [ "$publishTargetFramework" == "netcoreapp3.0" ]
then
    dotnet publish -f $publishTargetFramework -c $buildConfiguration test/test-applications/integrations/Samples.AspNetCoreMvc30/Samples.AspNetCoreMvc30.csproj -p:Configuration=$buildConfiguration -p:ManagedProfilerOutputDirectory="$PUBLISH_OUTPUT"
fi

# Only build Samples.AspNetCoreMvc31 for netcoreapp3.1
if [ "$publishTargetFramework" == "netcoreapp3.1" ]
then
    dotnet publish -f $publishTargetFramework -c $buildConfiguration test/test-applications/integrations/Samples.AspNetCoreMvc31/Samples.AspNetCoreMvc31.csproj -p:Configuration=$buildConfiguration -p:ManagedProfilerOutputDirectory="$PUBLISH_OUTPUT"
fi

for sample in Samples.Elasticsearch Samples.Elasticsearch.V5 Samples.ServiceStack.Redis Samples.StackExchange.Redis Samples.SqlServer Samples.Microsoft.Data.SqlClient Samples.MongoDB Samples.HttpMessageHandler Samples.WebRequest Samples.Npgsql Samples.MySql Samples.GraphQL Samples.FakeKudu Samples.Dapper Samples.NoMultiLoader Samples.RabbitMQ ; do
    dotnet publish -f $publishTargetFramework -c $buildConfiguration test/test-applications/integrations/$sample/$sample.csproj -p:Configuration=$buildConfiguration -p:ManagedProfilerOutputDirectory="$PUBLISH_OUTPUT"
done

for sample in OrleansCrash DataDogThreadTest HttpMessageHandler.StackOverflowException StackExchange.Redis.StackOverflowException AspNetMvcCorePerformance AssemblyLoad.FileNotFoundException TraceContext.InvalidOperationException AssemblyResolveMscorlibResources.InfiniteRecursionCrash StackExchange.Redis.AssemblyConflict.SdkProject NetCoreAssemblyLoadFailureOlderNuGet ; do
    dotnet publish -f $publishTargetFramework -c $buildConfiguration test/test-applications/regression/$sample/$sample.csproj -p:Configuration=$buildConfiguration -p:ManagedProfilerOutputDirectory="$PUBLISH_OUTPUT"
done

dotnet msbuild Datadog.Trace.proj -t:RestoreAndBuildSamplesForPackageVersions -p:Configuration=$buildConfiguration -p:ManagedProfilerOutputDirectory="$PUBLISH_OUTPUT" -p:TargetFramework=$publishTargetFramework

for proj in Datadog.Trace.IntegrationTests Datadog.Trace.OpenTracing.IntegrationTests Datadog.Trace.ClrProfiler.IntegrationTests ; do
    dotnet publish -f $publishTargetFramework -c $buildConfiguration test/$proj/$proj.csproj
done

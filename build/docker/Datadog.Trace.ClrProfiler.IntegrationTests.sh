#!/bin/bash
set -euxo pipefail

cd "$( dirname "${BASH_SOURCE[0]}" )"/../../

mkdir -p /var/log/datadog/dotnet
touch /var/log/datadog/dotnet/dotnet-tracer-native.log

dotnet vstest test/Datadog.Trace.IntegrationTests/bin/$buildConfiguration/$publishTargetFramework/publish/Datadog.Trace.IntegrationTests.dll --logger:trx --ResultsDirectory:test/Datadog.Trace.IntegrationTests/results

dotnet vstest test/Datadog.Trace.OpenTracing.IntegrationTests/bin/$buildConfiguration/$publishTargetFramework/publish/Datadog.Trace.OpenTracing.IntegrationTests.dll --logger:trx --ResultsDirectory:test/Datadog.Trace.OpenTracing.IntegrationTests/results

# servicestackredis
wait-for-it host.docker.internal:6379 -- \

# stackexchangeredis
wait-for-it host.docker.internal:6379 -- \

# elasticsearch6
wait-for-it host.docker.internal:9200 -- \

# elasticsearch5
wait-for-it host.docker.internal:9205 -- \

# sqlserver
wait-for-it host.docker.internal:1433 -- \

# postgres
wait-for-it host.docker.internal:5432 -- \

dotnet vstest test/Datadog.Trace.ClrProfiler.IntegrationTests/bin/$buildConfiguration/$publishTargetFramework/publish/Datadog.Trace.ClrProfiler.IntegrationTests.dll --logger:trx --ResultsDirectory:test/Datadog.Trace.ClrProfiler.IntegrationTests/results

cp /var/log/datadog/dotnet/dotnet-tracer-native.log /project/

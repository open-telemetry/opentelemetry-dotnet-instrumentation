#!/bin/bash
set -euxo pipefail

cd "$( dirname "${BASH_SOURCE[0]}" )"/../../

mkdir -p /var/log/datadog/dotnet
touch /var/log/datadog/dotnet/dotnet-tracer-native.log

dotnet vstest test/Datadog.Trace.IntegrationTests/bin/$buildConfiguration/$publishTargetFramework/publish/Datadog.Trace.IntegrationTests.dll --logger:trx --ResultsDirectory:test/Datadog.Trace.IntegrationTests/results

dotnet vstest test/Datadog.Trace.OpenTracing.IntegrationTests/bin/$buildConfiguration/$publishTargetFramework/publish/Datadog.Trace.OpenTracing.IntegrationTests.dll --logger:trx --ResultsDirectory:test/Datadog.Trace.OpenTracing.IntegrationTests/results

wait-for-it servicestackredis:6379 -- \
wait-for-it stackexchangeredis:6379 -- \
wait-for-it elasticsearch7_arm64:9200 -- \
wait-for-it sqledge:1433 -- \
wait-for-it mongo:27017 -- \
wait-for-it postgres:5432 -- \
dotnet vstest test/Datadog.Trace.ClrProfiler.IntegrationTests/bin/$buildConfiguration/$publishTargetFramework/publish/Datadog.Trace.ClrProfiler.IntegrationTests.dll --logger:trx --ResultsDirectory:test/Datadog.Trace.ClrProfiler.IntegrationTests/results --TestCaseFilter:Category!=ArmUnsupported

cp /var/log/datadog/dotnet/dotnet-tracer-native.log /project/

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using My.Custom.Test.Namespace;

if (args.Contains("--runtime-native-methods"))
{
    TestApplication.ContinuousProfiler.RuntimeReconfigurationScenario.VerifyNativeMethodsContract();
    return;
}

if (args.Contains("--runtime-unprepared-pipelines"))
{
    TestApplication.ContinuousProfiler.RuntimeReconfigurationScenario.VerifyUnpreparedPipelinesAreRejected();
    return;
}

using ActivitySource activitySource = new("TestApplication.ContinuousProfiler", "1.0.0");

using var activity = activitySource.StartActivity();
ClassA.MethodA();

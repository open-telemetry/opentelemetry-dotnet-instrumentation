// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using My.Custom.Test.Namespace;

if (args.Contains("--runtime-reconfiguration"))
{
    TestApplication.ContinuousProfiler.RuntimeReconfigurationScenario.Run();
    return;
}

if (args.Contains("--runtime-unprepared-thread"))
{
    TestApplication.ContinuousProfiler.RuntimeReconfigurationScenario.VerifyUnpreparedThreadSamplingIsRejected();
    return;
}

#if NET
if (args.Contains("--runtime-unprepared-allocation"))
{
    TestApplication.ContinuousProfiler.RuntimeReconfigurationScenario.VerifyUnpreparedAllocationIsRejected();
    return;
}
#endif

using ActivitySource activitySource = new("TestApplication.ContinuousProfiler", "1.0.0");

using var activity = activitySource.StartActivity();
ClassA.MethodA();

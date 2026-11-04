// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using My.Custom.Test.Namespace;
using TestApplication.ContinuousProfiler;

using ActivitySource activitySource = new("TestApplication.ContinuousProfiler", "1.0.0");

using var activity = activitySource.StartActivity();

if (string.Equals(Environment.GetEnvironmentVariable("OTEL_TEST_RUNTIME_SAMPLER_TRANSITIONS"), "true", StringComparison.Ordinal))
{
    RuntimeSamplerTransitions.Run();
    return;
}

if (string.Equals(Environment.GetEnvironmentVariable("OTEL_TEST_DEEP_STACK"), "true", StringComparison.Ordinal))
{
    _ = DeepStack.Capture(600);
}
else
{
    ClassA.MethodA();
}

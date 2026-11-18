// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Instrumentations.SqlClient;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.SqlClient;

[Collection("Non-Parallel Collection")]
public class SqlClientActivityCaptureTests
{
    private const string SourceName = "OpenTelemetry.Instrumentation.SqlClient";

    [Fact]
    public void CapturesSqlContextAfterCurrentActivityIsRestored()
    {
        using var sampler = CreateSamplingListener();
        using var observer = SqlClientTraceContextPropagator.CreateActivityListener();
        using var source = new ActivitySource(SourceName);
        using var otherSource = new ActivitySource("Other.Database");
        using var caller = new Activity("caller");
        caller.SetIdFormat(ActivityIdFormat.W3C);
        caller.Start();
        var capture = SqlClientTraceContextPropagator.BeginCapture();
        try
        {
            using var sqlActivity = source.StartActivity("query", ActivityKind.Client);
            Assert.NotNull(sqlActivity);

            // SqlClient 1.20.2 restores the caller before WriteBeginExecuteEvent returns.
            Activity.Current = caller;
            using var otherActivity = otherSource.StartActivity("other query");

            Assert.Equal(sqlActivity.Context, capture.Context);
        }
        finally
        {
            SqlClientTraceContextPropagator.EndCapture(capture);
        }
    }

    [Fact]
    public void RestoresCaptureAfterNestedCommand()
    {
        using var sampler = CreateSamplingListener();
        using var observer = SqlClientTraceContextPropagator.CreateActivityListener();
        using var source = new ActivitySource(SourceName);
        var outer = SqlClientTraceContextPropagator.BeginCapture();
        try
        {
            var inner = SqlClientTraceContextPropagator.BeginCapture();
            try
            {
                using var innerActivity = source.StartActivity("inner query");
                Assert.NotNull(innerActivity);
                Assert.Equal(innerActivity.Context, inner.Context);
                Assert.Equal(default, outer.Context);
            }
            finally
            {
                SqlClientTraceContextPropagator.EndCapture(inner);
            }

            using var outerActivity = source.StartActivity("outer query");
            Assert.NotNull(outerActivity);
            Assert.Equal(outerActivity.Context, outer.Context);
        }
        finally
        {
            SqlClientTraceContextPropagator.EndCapture(outer);
        }
    }

    [Fact]
    public void DoesNotCreateActivitiesWithoutASamplingListener()
    {
        using var observer = SqlClientTraceContextPropagator.CreateActivityListener();
        using var source = new ActivitySource(SourceName);

        using var activity = source.StartActivity("query");

        Assert.Null(activity);
    }

    private static ActivityListener CreateSamplingListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = static _ => true,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
#endif

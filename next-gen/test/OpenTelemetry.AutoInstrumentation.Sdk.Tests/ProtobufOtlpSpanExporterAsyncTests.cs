// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpenTelemetryProtocol.Tracing;
using OpenTelemetry.Tracing;
using Xunit;

using OtlpCommon = OpenTelemetry.Proto.Common.V1;
using OtlpTrace = OpenTelemetry.Proto.Trace.V1;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests;

public sealed class ProtobufOtlpSpanExporterAsyncTests : IDisposable
{
    private readonly ActivityListener _ActivityListener;
    private readonly ActivityIdFormat _PreviousDefaultIdFormat;
    private readonly bool _PreviousForceDefaultIdFormat;

    public ProtobufOtlpSpanExporterAsyncTests()
    {
        // Store previous global settings
        _PreviousDefaultIdFormat = Activity.DefaultIdFormat;
        _PreviousForceDefaultIdFormat = Activity.ForceDefaultIdFormat;

        // Set test-specific global settings
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        _ActivityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => options.Parent.TraceFlags.HasFlag(ActivityTraceFlags.Recorded)
                ? ActivitySamplingResult.AllDataAndRecorded
                : ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(_ActivityListener);
    }

    public void Dispose()
    {
        _ActivityListener.Dispose();

        // Restore previous global settings
        Activity.DefaultIdFormat = _PreviousDefaultIdFormat;
        Activity.ForceDefaultIdFormat = _PreviousForceDefaultIdFormat;
    }

    [Fact]
    public void WriteSpanTest()
    {
        using var activitySource = new ActivitySource(nameof(this.WriteSpanTest));
        using Activity? rootActivity = activitySource.StartActivity("root", ActivityKind.Producer);
        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("bool", true),
            new("long", 1L),
            new("string", "text"),
            new("double", 3.14),
            new("int", 1),
            new("datetime", DateTime.UtcNow),
            new("bool_array", new bool[] { true, false }),
            new("int_array", new int[] { 1, 2 }),
            new("double_array", new double[] { 1.0, 2.09 }),
            new("string_array", new string[] { "a", "b" }),
            new("datetime_array", new DateTime[] { DateTime.UtcNow, DateTime.Now }),
        };

        Assert.NotNull(rootActivity);
        foreach (KeyValuePair<string, object?> kvp in attributes)
        {
            rootActivity.SetTag(kvp.Key, kvp.Value);
        }

        var startTime = new DateTime(2020, 02, 20, 20, 20, 20, DateTimeKind.Utc);

        DateTimeOffset dateTimeOffset;
        dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(0);

        ulong expectedUnixTimeTicks = (ulong)(startTime.Ticks - dateTimeOffset.Ticks);
        var duration = TimeSpan.FromMilliseconds(1555);

        rootActivity.SetStartTime(startTime);
        rootActivity.SetEndTime(startTime + duration);

        Span<byte> traceIdSpan = stackalloc byte[16];
        rootActivity.TraceId.CopyTo(traceIdSpan);
        byte[] traceId = traceIdSpan.ToArray();

        OtlpTrace.Span? otlpSpan = ToOtlpSpan(rootActivity);

        Assert.NotNull(otlpSpan);
        Assert.Equal("root", otlpSpan.Name);
        Assert.Equal(OtlpTrace.Span.Types.SpanKind.Producer, otlpSpan.Kind);
        Assert.Equal(traceId, otlpSpan.TraceId);
        Assert.Empty(otlpSpan.ParentSpanId);
        Assert.Null(otlpSpan.Status);
        Assert.Empty(otlpSpan.Events);
        Assert.Empty(otlpSpan.Links);
        OtlpTestHelpers.AssertOtlpAttributes(attributes, otlpSpan.Attributes);

        ulong expectedStartTimeUnixNano = 100 * expectedUnixTimeTicks;
        Assert.Equal(expectedStartTimeUnixNano, otlpSpan.StartTimeUnixNano);
        double expectedEndTimeUnixNano = expectedStartTimeUnixNano + (duration.TotalMilliseconds * 1_000_000);
        Assert.Equal(expectedEndTimeUnixNano, otlpSpan.EndTimeUnixNano);

        var childLinks = new List<ActivityLink> { new(rootActivity.Context, new ActivityTagsCollection(attributes)) };
        Activity? childActivity = activitySource.StartActivity(
            "child",
            ActivityKind.Client,
            rootActivity.Context,
            links: childLinks);

        Assert.NotNull(childActivity);

        childActivity.SetStatus(ActivityStatusCode.Error, new string('a', 150));

        var childEvents = new List<ActivityEvent> { new("e0"), new("e1", default, new ActivityTagsCollection(attributes)) };
        childActivity.AddEvent(childEvents[0]);
        childActivity.AddEvent(childEvents[1]);

        Span<byte> parentIdSpan = stackalloc byte[8];
        rootActivity.Context.SpanId.CopyTo(parentIdSpan);
        byte[] parentId = parentIdSpan.ToArray();

        otlpSpan = ToOtlpSpan(childActivity);

        Assert.NotNull(otlpSpan);
        Assert.Equal("child", otlpSpan.Name);
        Assert.Equal(OtlpTrace.Span.Types.SpanKind.Client, otlpSpan.Kind);
        Assert.Equal(traceId, otlpSpan.TraceId);
        Assert.Equal(parentId, otlpSpan.ParentSpanId);

        Assert.NotNull(otlpSpan.Status);
        Assert.Equal(OtlpTrace.Status.Types.StatusCode.Error, otlpSpan.Status.Code);

        Assert.Equal(childActivity.StatusDescription ?? string.Empty, otlpSpan.Status.Message);
        Assert.Empty(otlpSpan.Attributes);

        Assert.Equal(childEvents.Count, otlpSpan.Events.Count);
        for (int i = 0; i < childEvents.Count; i++)
        {
            Assert.Equal(childEvents[i].Name, otlpSpan.Events[i].Name);
            OtlpTestHelpers.AssertOtlpAttributes(childEvents[i].Tags.ToList(), otlpSpan.Events[i].Attributes);
        }

        childLinks.Reverse();
        Assert.Equal(childLinks.Count, otlpSpan.Links.Count);
        for (int i = 0; i < childLinks.Count; i++)
        {
            IEnumerable<KeyValuePair<string, object?>>? tags = childLinks[i].Tags;
            Assert.NotNull(tags);
            OtlpTestHelpers.AssertOtlpAttributes(tags, otlpSpan.Links[i].Attributes);
        }

        var flags = (OtlpTrace.SpanFlags)otlpSpan.Flags;
        Assert.True(flags.HasFlag(OtlpTrace.SpanFlags.ContextHasIsRemoteMask));
        Assert.False(flags.HasFlag(OtlpTrace.SpanFlags.ContextIsRemoteMask));
    }

    [Fact]
    public void ToOtlpSpanActivitiesWithNullArrayTest()
    {
        using var activitySource = new ActivitySource(nameof(this.ToOtlpSpanActivitiesWithNullArrayTest));

        using Activity? rootActivity = activitySource.StartActivity("root", ActivityKind.Client);
        Assert.NotNull(rootActivity);

        string?[] stringArr = new string?[] { "test", string.Empty, null };
        rootActivity.SetTag("stringArray", stringArr);

        OtlpTrace.Span? otlpSpan = ToOtlpSpan(rootActivity);

        Assert.NotNull(otlpSpan);

        OtlpCommon.KeyValue? stringArray = otlpSpan.Attributes.FirstOrDefault(kvp => kvp.Key == "stringArray");

        Assert.NotNull(stringArray);
        Assert.Equal("test", stringArray.Value.ArrayValue.Values[0].StringValue);
        Assert.Equal(string.Empty, stringArray.Value.ArrayValue.Values[1].StringValue);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.None, stringArray.Value.ArrayValue.Values[2].ValueCase);
    }

    [Theory]
    [InlineData(ActivityStatusCode.Unset, "Description will be ignored if status is Unset.")]
    [InlineData(ActivityStatusCode.Ok, "Description will be ignored if status is Okay.")]
    [InlineData(ActivityStatusCode.Error, "Description will be kept if status is Error.")]
    [InlineData(ActivityStatusCode.Error, "150 Character String - aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void ToOtlpSpanNativeActivityStatusTest(ActivityStatusCode expectedStatusCode, string statusDescription)
    {
        using var activitySource = new ActivitySource(nameof(this.ToOtlpSpanNativeActivityStatusTest));
        using Activity? activity = activitySource.StartActivity("Name");
        Assert.NotNull(activity);
        activity.SetStatus(expectedStatusCode, statusDescription);

        OtlpTrace.Span? otlpSpan = ToOtlpSpan(activity);
        Assert.NotNull(otlpSpan);
        if (expectedStatusCode == ActivityStatusCode.Unset)
        {
            Assert.Null(otlpSpan.Status);
        }
        else
        {
            Assert.NotNull(otlpSpan.Status);
            Assert.Equal((int)expectedStatusCode, (int)otlpSpan.Status.Code);
            if (expectedStatusCode == ActivityStatusCode.Error)
            {
                Assert.Equal(statusDescription, otlpSpan.Status.Message);
            }

            if (expectedStatusCode == ActivityStatusCode.Ok)
            {
                Assert.Empty(otlpSpan.Status.Message);
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ToOtlpSpanTraceStateTest(bool traceStateWasSet)
    {
        using var activitySource = new ActivitySource(nameof(this.ToOtlpSpanTraceStateTest));
        using Activity? activity = activitySource.StartActivity("Name");
        Assert.NotNull(activity);
        string tracestate = "a=b;c=d";
        if (traceStateWasSet)
        {
            activity.TraceStateString = tracestate;
        }

        OtlpTrace.Span? otlpSpan = ToOtlpSpan(activity);
        Assert.NotNull(otlpSpan);

        if (traceStateWasSet)
        {
            Assert.NotNull(otlpSpan.TraceState);
            Assert.Equal(tracestate, otlpSpan.TraceState);
        }
        else
        {
            Assert.Equal(string.Empty, otlpSpan.TraceState);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SpanFlagsTest(bool isRecorded)
    {
        using var activitySource = new ActivitySource(nameof(this.SpanFlagsTest));

        ActivityContext ctx = new ActivityContext(
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            isRecorded ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None);

        using Activity? rootActivity = activitySource.StartActivity("root", ActivityKind.Server, ctx);
        Assert.NotNull(rootActivity);

        OtlpTrace.Span? otlpSpan = ToOtlpSpan(rootActivity);

        Assert.NotNull(otlpSpan);
        var flags = (OtlpTrace.SpanFlags)otlpSpan.Flags;

        ActivityTraceFlags traceFlags = (ActivityTraceFlags)(flags & OtlpTrace.SpanFlags.TraceFlagsMask);

        if (isRecorded)
        {
            Assert.True(traceFlags.HasFlag(ActivityTraceFlags.Recorded));
        }
        else
        {
            Assert.False(traceFlags.HasFlag(ActivityTraceFlags.Recorded));
        }

        Assert.True(flags.HasFlag(OtlpTrace.SpanFlags.ContextHasIsRemoteMask));
    }

    private static OtlpTrace.Span? ToOtlpSpan(Activity activity)
    {
        OtlpSpanExporterAsync.OtlpSpanWriter spanWriter = new OtlpSpanExporterAsync.OtlpSpanWriter();
        UseActivityAsSpan(activity, span =>
        {
            spanWriter.WriteSpan(in span);
        });

        using var stream = new MemoryStream(spanWriter.Request.Buffer, 0, spanWriter.Request.WritePosition);
        OtlpTrace.ScopeSpans scopeSpans = OtlpTrace.ScopeSpans.Parser.ParseFrom(stream);
        return scopeSpans.Spans.FirstOrDefault();
    }

    private static void UseActivityAsSpan(Activity activity, Action<Span> spanAction)
    {
        var scope = new InstrumentationScope(activity.Source.Name)
        {
            Version = activity.Source.Version
        };

        var spanInfo = new SpanInfo(
           scope,
           name: activity.DisplayName ?? activity.OperationName)
        {
            TraceId = activity.TraceId,
            SpanId = activity.SpanId,
            TraceFlags = activity.ActivityTraceFlags,
            TraceState = activity.TraceStateString,
            ParentSpanId = activity.ParentSpanId,
            Kind = activity.Kind,
            StartTimestampUtc = activity.StartTimeUtc,
            EndTimestampUtc = activity.StartTimeUtc.Add(activity.Duration),
            StatusCode = activity.Status,
            StatusDescription = activity.StatusDescription
        };

        SpanEvent[] spanEvents = activity.Events.Select(e => new SpanEvent(e.Name, e.Timestamp.UtcDateTime, e.Tags.ToArray())).ToArray();
        SpanLink[] spanLinks = activity.Links
            .Select(l => new SpanLink(l.Context, l.Tags?.ToArray() ?? Array.Empty<KeyValuePair<string, object?>>()))
            .ToArray();

        var span = new Span(in spanInfo)
        {
            Attributes = activity.TagObjects.ToArray(),
            Events = spanEvents,
            Links = spanLinks,
        };

        spanAction(span);
    }
}

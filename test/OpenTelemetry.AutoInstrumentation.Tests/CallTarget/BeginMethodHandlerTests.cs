// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Tests.CallTarget;

public class BeginMethodHandlerTests
{
    private static readonly ActivityContext ParentContext = new(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded, "vendor=value", isRemote: true);
    private static readonly DateTimeOffset ActivityStartTime = new(2026, 11, 9, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NineArgumentsPreserveInputsAndPropagateReferenceAndValueTypeChanges()
    {
        using var previousActivity = new Activity("previous");
        previousActivity.Start();
        var target = new object();
        string? name = "operation";
        var kind = ActivityKind.Client;
        var context = default(ActivityContext);
        string? parentId = "explicit-parent";
        IEnumerable<KeyValuePair<string, object?>>? tags = [new("test", 1)];
        IEnumerable<ActivityLink>? links = null;
        var startTime = ActivityStartTime;
        var startIt = true;
        var idFormat = ActivityIdFormat.Unknown;

        var state = CallTargetInvoker.BeginMethod<NineArgumentIntegration, object, string, ActivityKind, ActivityContext, string, IEnumerable<KeyValuePair<string, object?>>, IEnumerable<ActivityLink>, DateTimeOffset, bool, ActivityIdFormat>(
            target, ref name, ref kind, ref context, ref parentId, ref tags, ref links, ref startTime, ref startIt, ref idFormat);

        Assert.Same(target, state.State);
        Assert.Same(previousActivity, state.PreviousActivity);
        Assert.Equal(ParentContext, context);
        Assert.Equal(ParentContext, Assert.Single(links!).Context);
        Assert.Equal(ActivityIdFormat.W3C, idFormat);
        Assert.Same(previousActivity, Activity.Current);
    }

#pragma warning disable CA1812 // The integration is instantiated indirectly as a CallTarget generic argument.
    private sealed class NineArgumentIntegration
    {
        internal static CallTargetState OnMethodBegin<TTarget>(
            TTarget instance,
            string? name,
            ActivityKind kind,
            ref ActivityContext context,
            string? parentId,
            IEnumerable<KeyValuePair<string, object?>>? tags,
            ref IEnumerable<ActivityLink>? links,
            DateTimeOffset startTime,
            bool startIt,
            ref ActivityIdFormat idFormat)
        {
            Assert.Equal("operation", name);
            Assert.Equal(ActivityKind.Client, kind);
            Assert.Equal(default, context);
            Assert.Equal("explicit-parent", parentId);
            Assert.Equal(new KeyValuePair<string, object?>("test", 1), Assert.Single(tags!));
            Assert.Null(links);
            Assert.Equal(ActivityStartTime, startTime);
            Assert.True(startIt);
            Assert.Equal(ActivityIdFormat.Unknown, idFormat);

            context = ParentContext;
            links = [new ActivityLink(ParentContext)];
            idFormat = ActivityIdFormat.W3C;
            return new CallTargetState(null, instance);
        }
    }
#pragma warning restore CA1812 // The integration is instantiated indirectly as a CallTarget generic argument.
}

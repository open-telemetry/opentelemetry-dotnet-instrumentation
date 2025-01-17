// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using NSubstitute;
using OpenTelemetry.AutoInstrumentation.Tagging;
using OpenTelemetry.AutoInstrumentation.Util;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Util;

public class ActivityHelperTests
{
    [Fact]
    public void SetException_NotThrow_WhenActivityIsNull()
    {
        const Activity? activity = null;

        var action = () => activity.SetException(new Exception());

        Assert.Null(Record.Exception(() => action()));
    }

    [Fact]
    public void SetException_NotThrow_WhenExceptionIsNull()
    {
        var activity = new Activity("test-operation");

        var action = () =>
        {
            activity.SetException(null);
            activity.Dispose();
        };

        Assert.Null(Record.Exception(() => action()));
    }

    [Fact]
    public void SetException_SetsExceptionDetails()
    {
        using var activity = new Activity("test-operation");

        var exceptionMessage = "test-message";
        activity.SetException(new Exception(exceptionMessage));

        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal(exceptionMessage, activity.StatusDescription);
        Assert.Single(activity.Events);
    }

    [Fact]
    public void StartActivityWithTags_ReturnsNull_WhenActivitySourceIsNull()
    {
        const ActivitySource? activitySource = null;

        using var activity = activitySource.StartActivityWithTags("test-operation", ActivityKind.Internal, Substitute.For<ITags>());

        Assert.Null(activity);
    }

    [Fact]
    public void StartActivityWithTags_ReturnsNull_WhenActivitySourceDoesNotHaveListener()
    {
        using var activitySource = new ActivitySource("test-source");

        using var activity = activitySource.StartActivityWithTags("test-operation", ActivityKind.Internal, Substitute.For<ITags>());

        Assert.False(activitySource.HasListeners());
        Assert.Null(activity);
    }

    [Theory]
    [InlineData(ActivityKind.Internal)]
    [InlineData(ActivityKind.Server)]
    [InlineData(ActivityKind.Client)]
    [InlineData(ActivityKind.Producer)]
    [InlineData(ActivityKind.Consumer)]
    public void StartActivityWithTags_ReturnsActivity_WhenThereIsActivityListener(ActivityKind kind)
    {
        var tagsMock = Substitute.For<ITags>();
        tagsMock.GetAllTags().Returns(new List<KeyValuePair<string, string>>());

        using var activitySource = new ActivitySource("test-source");

        using var listener = CreateActivityListener(activitySource);

        using var activity = activitySource.StartActivityWithTags("test-operation", kind, tagsMock);

        Assert.True(activitySource.HasListeners());
        Assert.NotNull(activity);
        Assert.Equal(kind, activity.Kind);
    }

    [Fact]
    public void StartActivityWithTags_SetsCorrectTags()
    {
        var tags = new List<KeyValuePair<string, string>>
        {
            new("key1", "value1"),
            new("key2", "value2")
        };

        var tagsMock = Substitute.For<ITags>();
        tagsMock.GetAllTags().Returns(tags);

        using var activitySource = new ActivitySource("test-source");

        using var listener = CreateActivityListener(activitySource);

        using var activity = activitySource.StartActivityWithTags("test-operation", ActivityKind.Internal, tagsMock);

        tagsMock.GetAllTags().Returns(tags);

        Assert.True(activitySource.HasListeners());
        Assert.NotNull(activity);
        Assert.Equal(tags.Cast<KeyValuePair<string, string?>>(), activity.Tags);
    }

    private static ActivityListener CreateActivityListener(ActivitySource activitySource)
    {
        var listener = new ActivityListener();
        listener.ShouldListenTo = source => source == activitySource;
        listener.Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData;
        ActivitySource.AddActivityListener(listener);

        return listener;
    }
}

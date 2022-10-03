// <copyright file="ActivityHelperTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using OpenTelemetry.AutoInstrumentation.Tagging;
using OpenTelemetry.AutoInstrumentation.Util;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Util;

public class ActivityHelperTests
{
    [Fact]
    public void SetException_NotThrow_WhenActivityIsNull()
    {
        const Activity activity = null;

        var action = () => activity.SetException(new Exception());

        action.Should().NotThrow();
    }

    [Fact]
    public void SetException_NotThrow_WhenExceptionIsNull()
    {
        var activity = new Activity("test-operation");

        var action = () => activity.SetException(null);

        action.Should().NotThrow();
    }

    [Fact]
    public void SetException_SetsExceptionDetails()
    {
        var activity = new Activity("test-operation");

        var exceptionMessage = "test-message";
        activity.SetException(new Exception(exceptionMessage));

        using (new AssertionScope())
        {
            activity.Tags.First(x => x.Key == "otel.status_code").Value.Should().Be("ERROR");
            activity.Tags.First(x => x.Key == "otel.status_description").Value.Should().Be(exceptionMessage);
            activity.Events.Should().HaveCount(1);
        }
    }

    [Fact]
    public void StartActivityWithTags_ReturnsNull_WhenActivitySourceIsNull()
    {
        const ActivitySource activitySource = null;

        var activity = activitySource.StartActivityWithTags("test-operation", ActivityKind.Internal, null);

        activity.Should().BeNull();
    }

    [Fact]
    public void StartActivityWithTags_ReturnsNull_WhenActivitySourceDoesNotHaveListener()
    {
        var activitySource = new ActivitySource("test-source");

        var activity = activitySource.StartActivityWithTags("test-operation", ActivityKind.Internal, null);

        using (new AssertionScope())
        {
            activitySource.HasListeners().Should().BeFalse();
            activity.Should().BeNull();
        }
    }

    [Theory]
    [InlineData(ActivityKind.Internal)]
    [InlineData(ActivityKind.Server)]
    [InlineData(ActivityKind.Client)]
    [InlineData(ActivityKind.Producer)]
    [InlineData(ActivityKind.Consumer)]
    public void StartActivityWithTags_ReturnsActivity_WhenThereIsActivityListener(ActivityKind kind)
    {
        using var listener = CreateActivityListener();

        var activitySource = new ActivitySource("test-source");
        var activity = activitySource.StartActivityWithTags("test-operation", kind, null);

        using (new AssertionScope())
        {
            activitySource.HasListeners().Should().BeTrue();
            activity.Should().NotBeNull();
            activity.Kind.Should().Be(kind);
        }
    }

    [Fact]
    public void StartActivityWithTags_SetsCorrectTags()
    {
        using var listener = CreateActivityListener();

        var tags = new List<KeyValuePair<string, string>>
        {
            new("key1", "value1"),
            new("key2", "value2")
        };

        var tagsMock = new Mock<ITags>();
        tagsMock.Setup(x => x.GetAllTags()).Returns(tags);

        var activitySource = new ActivitySource("test-source");
        var activity = activitySource.StartActivityWithTags("test-operation", ActivityKind.Internal, tagsMock.Object);

        tagsMock.Setup(x => x.GetAllTags()).Returns(tags);

        using (new AssertionScope())
        {
            activitySource.HasListeners().Should().BeTrue();
            activity.Should().NotBeNull();
            activity.Tags.Should().BeEquivalentTo(tags);
        }
    }

    private static ActivityListener CreateActivityListener()
    {
        var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData;
        ActivitySource.AddActivityListener(listener);

        return listener;
    }
}

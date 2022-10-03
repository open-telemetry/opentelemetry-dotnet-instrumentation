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
using System.Diagnostics;
using FluentAssertions;
using FluentAssertions.Execution;
using OpenTelemetry.AutoInstrumentation.Util;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Util;

public class ActivityHelperTests : IDisposable
{
    private static ActivitySamplingResult _activitySamplingResult = ActivitySamplingResult.None;
    private static bool _shouldSample = false;

    static ActivityHelperTests()
    {
        var listener = new ActivityListener();
        listener.ShouldListenTo = _ => _shouldSample;
        listener.Sample = (ref ActivityCreationOptions<ActivityContext> _) => _activitySamplingResult;
        ActivitySource.AddActivityListener(listener);
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
        EnableActivityListener();

        var activitySource = new ActivitySource("test-source");
        var activity = activitySource.StartActivityWithTags("test-operation", kind, null);

        using (new AssertionScope())
        {
            activitySource.HasListeners().Should().BeTrue();
            activity.Should().NotBeNull();
            activity.Kind.Should().Be(kind);
        }
    }

    public void Dispose()
    {
        _activitySamplingResult = ActivitySamplingResult.None;
        _shouldSample = false;
    }

    private static void EnableActivityListener()
    {
        _shouldSample = true;
        _activitySamplingResult = ActivitySamplingResult.AllData;
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Util;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Util;

public class ActivityHelperTests
{
    [Fact]
    public void SetException_NotThrow_WhenActivityIsNull()
    {
        const Activity? activity = null;

        var action = () => activity.SetException(new InvalidOperationException());

        Assert.Null(Record.Exception(() => action()));
    }

    [Fact]
    public void SetException_NotThrow_WhenExceptionIsNull()
    {
        using var activity = new Activity("test-operation");

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

        const string exceptionMessage = "test-message";
        activity.SetException(new InvalidOperationException(exceptionMessage));

        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal(exceptionMessage, activity.StatusDescription);
        Assert.Single(activity.Events);
    }
}

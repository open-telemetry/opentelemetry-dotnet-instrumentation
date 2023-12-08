// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Tagging;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal static class ActivityHelper
{
    private static readonly IOtelLogger Log = OtelLogging.GetLogger();

    /// <summary>
    /// Add the StackTrace and other exception metadata to the span
    /// </summary>
    /// <param name="activity">The activity to include exception info.</param>
    /// <param name="exception">The exception.</param>
    public static void SetException(this Activity? activity, Exception? exception)
    {
        if (activity == null)
        {
            Log.Debug("Trying to set exception on null activity.");
            return;
        }

        if (exception == null)
        {
            return;
        }

        activity.SetStatus(Status.Error.WithDescription(exception.Message));
        activity.RecordException(exception);
    }

    public static Activity? StartActivityWithTags(this ActivitySource? activitySource, string operationName, ActivityKind kind, ITags tags)
    {
        if (activitySource == null)
        {
            Log.Debug("Trying to start activity on null activity source.");
            return null;
        }

        var activity = activitySource.StartActivity(operationName, kind);

        if (activity == null)
        {
            return activity;
        }

        // Apply tags
        foreach (var entry in tags.GetAllTags())
        {
            activity.SetTag(entry.Key, entry.Value);
        }

        return activity;
    }
}

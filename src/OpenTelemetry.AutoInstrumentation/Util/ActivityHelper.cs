// <copyright file="ActivityHelper.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Tagging;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Util;

#nullable enable

internal static class ActivityHelper
{
    private static readonly ILogger Log = OtelLogging.GetLogger();

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

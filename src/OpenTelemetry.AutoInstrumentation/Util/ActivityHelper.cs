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
using OpenTelemetry.AutoInstrumentation.Tagging;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal static class ActivityHelper
{
    /// <summary>
    /// Set span (activity) resource name.
    /// </summary>
    /// <param name="activity">The activity to include resource name.</param>
    /// <param name="resourceName">The resource name</param>
    public static void SetResourceName(this Activity activity, string resourceName)
    {
        if (!string.IsNullOrWhiteSpace(resourceName))
        {
            activity.SetTag(Tags.ResourceName, resourceName);
        }
    }

    /// <summary>
    /// Add the StackTrace and other exception metadata to the span
    /// </summary>
    /// <param name="activity">The activity to include exception info.</param>
    /// <param name="exception">The exception.</param>
    public static void SetException(this Activity activity, Exception exception)
    {
        if (exception != null)
        {
            activity.SetStatus(Status.Error.WithDescription(exception.Message));
            activity.RecordException(exception);
        }
    }

    public static Activity StartActivityWithTags(this ActivitySource activitySource, string operationName, ITags tags)
    {
        var activity = activitySource.StartActivity(operationName);

        // Apply tags
        if (tags != null)
        {
            foreach (var entry in tags.GetAllTags())
            {
                activity.SetTag(entry.Key, entry.Value);
            }
        }

        return activity;
    }

    internal static void DisposeWithException(this Activity activity, Exception exception)
    {
        if (activity != null)
        {
            try
            {
                if (exception != null)
                {
                    activity.SetException(exception);
                }
            }
            finally
            {
                activity.Dispose();
            }
        }
    }
}

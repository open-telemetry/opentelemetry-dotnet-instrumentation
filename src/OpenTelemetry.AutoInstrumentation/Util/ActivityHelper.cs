// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Logging;

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

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddException(exception);
    }
}

using System;
using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Tagging;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Util
{
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

        public static Activity StartActivityWithTags(this ActivitySource activitySource, string operationName, string serviceName = null, ITags tags = null)
        {
            var settings = Instrumentation.TracerSettings;
            var activity = activitySource.StartActivity(operationName);

            // Apply tags
            if (tags != null)
            {
                foreach (var entry in tags.GetAllTags())
                {
                    activity.SetTag(entry.Key, entry.Value);
                }
            }

            // Apply any global tags
            if (settings.GlobalTags.Count > 0)
            {
                foreach (var entry in settings.GlobalTags)
                {
                    activity.SetTag(entry.Key, entry.Value);
                }
            }

            // automatically add the "env" tag if defined, taking precedence over an "env" tag set from a global tag
            var env = settings.Environment;
            if (!string.IsNullOrWhiteSpace(env))
            {
                activity.SetTag(Tags.Env, env);
            }

            // automatically add the "version" tag if defined, taking precedence over an "version" tag set from a global tag
            var version = settings.ServiceVersion;
            if (!string.IsNullOrWhiteSpace(version) && string.Equals(serviceName, settings.ServiceName))
            {
                activity.SetTag(Tags.Version, version);
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
}

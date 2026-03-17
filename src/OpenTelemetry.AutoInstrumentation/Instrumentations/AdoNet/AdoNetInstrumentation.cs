// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.AdoNet;

internal static class AdoNetInstrumentation
{
    private static readonly ActivitySource Source =
        new(new ActivitySourceOptions("OpenTelemetry.AutoInstrumentation.AdoNet")
        {
            Version = AutoInstrumentationVersion.Version,
            TelemetrySchemaUrl = "https://opentelemetry.io/schemas/1.40.0"
        });

    public static Activity? StartActivity<TTarget>(TTarget instance, string methodName)
    {
        return Source.StartActivity("TODO-SpanName", ActivityKind.Client);
    }

    public static void StopActivity(Activity? activity, Exception? exception)
    {
        if (activity is null)
        {
            return;
        }

        if (exception is not null)
        {
            activity.SetException(exception);
            activity.SetTag(GenericAttributes.Keys.ErrorType, exception.GetType().FullName);
        }

        activity.Stop();
    }
}

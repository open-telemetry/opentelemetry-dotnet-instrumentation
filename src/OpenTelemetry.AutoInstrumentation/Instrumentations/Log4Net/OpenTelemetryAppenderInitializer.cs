// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net;

internal static class OpenTelemetryAppenderInitializer<TAppenderArray>
{
    // ReSharper disable StaticMemberInGenericType
    private static readonly Type AppenderType;

    private static object? _otelAppender;

    static OpenTelemetryAppenderInitializer()
    {
        AppenderType = typeof(TAppenderArray).GetElementType()!;
    }

    public static TAppenderArray Initialize(Array initial, OpenTelemetryLog4NetAppender openTelemetryLog4NetAppender)
    {
        var newArray = Array.CreateInstance(AppenderType, initial.Length + 1);
        Array.Copy(initial, newArray, initial.Length);
        _otelAppender ??= openTelemetryLog4NetAppender.DuckImplement(AppenderType);

        newArray.SetValue(_otelAppender, newArray.Length - 1);
        return (TAppenderArray)(object)newArray;
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net.Bridge;

internal static class OpenTelemetryAppenderInitializer<TAppenderArray>
{
    private static readonly Type AppenderType = typeof(TAppenderArray).GetElementType()!;

    private static object? _otelAppender;

    public static TAppenderArray Initialize(Array initial)
    {
        var newArray = Array.CreateInstance(AppenderType, initial.Length + 1);
        Array.Copy(initial, newArray, initial.Length);
        _otelAppender ??= OpenTelemetryLog4NetAppender.Instance.DuckImplement(AppenderType);

        newArray.SetValue(_otelAppender, newArray.Length - 1);
        return (TAppenderArray)(object)newArray;
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms;

internal static class XmsMessageSetter
{
    public static readonly Action<IXmsMessage, string, string> Setter = SetProperty;

    private static void SetProperty(IXmsMessage carrier, string key, string value)
    {
        try
        {
            carrier.SetStringProperty(XmsMessagePropertyNames.Sanitize(key), value);
        }
        catch
        {
            // Instrumentation must never throw or break the customer application.
        }
    }
}

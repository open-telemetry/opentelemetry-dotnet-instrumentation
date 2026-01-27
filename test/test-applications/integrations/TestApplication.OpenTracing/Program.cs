// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTracing.Util;
using TestApplication.Shared;

namespace TestApplication.MySqlData;

internal static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        // Simulate typical usage of OpenTracing. Notice that the automatic instrumentation
        // must register the global tracer instance before any reference to OpenTracing.Util.GlobalTracer.Instance.
        var otTracer = GlobalTracer.Instance;
        using var otScopeManager = otTracer.BuildSpan("MySpan").StartActive();
        var otSpan = otScopeManager.Span;
        otSpan.SetTag("MyTag", "MyValue");
    }
}

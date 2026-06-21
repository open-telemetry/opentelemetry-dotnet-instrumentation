// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using TestApplication.Shared;

namespace TestApplication.TracesLegacySource;

internal sealed class Program
{
    public const string LegacySourceName = "ManualSpan";

    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        // legacy activity: created without an ActivitySource, so it is only collected
        // when registered via OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES
        using var activity = new Activity(LegacySourceName);
        activity.Start();
        activity.Stop();
    }
}

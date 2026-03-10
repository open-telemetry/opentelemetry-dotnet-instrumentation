// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Logging;

/// <summary>
/// Default logging sink that does nothing.
/// </summary>
internal class NoopSink : ISink
{
    public static readonly NoopSink Instance = new();

    private NoopSink()
    {
    }

    public void Write(string message)
    {
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Logging;

internal class ConsoleSink : ISink
{
    private readonly string _source;

    public ConsoleSink(string source)
    {
        _source = source;
    }

    public void Write(string message)
    {
        Console.WriteLine($"[{_source}] {message}");
    }
}

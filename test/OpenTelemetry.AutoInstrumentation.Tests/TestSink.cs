// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Tests;

internal sealed class TestSink : ISink
{
    public IList<string> Messages { get; } = new List<string>();

    public void Write(string message)
    {
        Messages.Add(message);
    }
}

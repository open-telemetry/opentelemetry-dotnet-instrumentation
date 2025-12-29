// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation;

internal static class Instrumentation
{
    public static void Initialize()
    {
        Console.WriteLine($"{nameof(Initialize)} called");
    }
}

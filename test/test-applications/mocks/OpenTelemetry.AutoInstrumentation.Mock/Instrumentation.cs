// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation;

public static class Instrumentation
{
    public static void Initialize()
    {
        Console.WriteLine($"{nameof(Initialize)} called");
    }
}

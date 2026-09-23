// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

internal static class OpAmpTestTimeouts
{
    public static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan LoaderShutdownTimeout = TimeSpan.FromMilliseconds(100);
}

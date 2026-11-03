// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

// Disposal must not wait for running callbacks.
internal interface IOpAmpReportTimer : IDisposable
{
    // Replace the deadline for one callback, without invoking it inline.
    void Schedule(TimeSpan delay);
}

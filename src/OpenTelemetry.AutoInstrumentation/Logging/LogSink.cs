// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Logging;

/// <summary>
/// Specifies the target sink.
/// </summary>
internal enum LogSink
{
    /// <summary>
    /// File sink.
    /// </summary>
    File,

    /// <summary>
    /// Std out or Console sink.
    /// </summary>
    Console,

    /// <summary>
    /// No op sink or null sink.
    /// </summary>
    NoOp
}

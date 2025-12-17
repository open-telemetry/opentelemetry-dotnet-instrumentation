// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Enum representing supported meter instrumentations.
/// </summary>
internal enum LogInstrumentation
{
    /// <summary>
    /// ILogger instrumentation.
    /// </summary>
    ILogger = 0,

    /// <summary>
    /// Log4Net instrumentation.
    /// </summary>
    Log4Net = 1,

    /// <summary>
    /// NLog instrumentation.
    /// </summary>
    NLog = 2,
}

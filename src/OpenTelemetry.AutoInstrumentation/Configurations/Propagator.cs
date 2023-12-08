// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Enum representing supported propagators.
/// </summary>
internal enum Propagator
{
    /// <summary>
    /// W3C Trace Context propagator.
    /// </summary>
    W3CTraceContext,

    /// <summary>
    /// W3C Baggage propagator.
    /// </summary>
    W3CBaggage,

    /// <summary>
    /// B3 multi propagator.
    /// </summary>
    B3Multi,

    /// <summary>
    /// B3 single propagator.
    /// </summary>
    B3Single
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Enum representing supported meter instrumentations.
/// </summary>
internal enum MetricInstrumentation
{
#if NETFRAMEWORK
    /// <summary>
    /// ASP.NET instrumentation.
    /// </summary>
    AspNet = 1,
#endif

    /// <summary>
    /// HttpClient instrumentation.
    /// </summary>
    HttpClient = 2,

    /// <summary>
    /// .NET Runtime Metrics instrumentation.
    /// </summary>
    NetRuntime = 3,

    /// <summary>
    /// Process instrumentation.
    /// </summary>
    Process = 4,

    /// <summary>
    /// NServiceBus instrumentation.
    /// </summary>
    NServiceBus = 5,

#if NET
    /// <summary>
    /// ASP.NET Core instrumentation.
    /// </summary>
    AspNetCore = 6,
#endif

    /// <summary>
    /// SqlClient instrumentation.
    /// </summary>
    SqlClient = 7
}

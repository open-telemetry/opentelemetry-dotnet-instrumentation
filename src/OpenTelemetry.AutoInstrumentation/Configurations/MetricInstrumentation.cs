// <copyright file="MetricInstrumentation.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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

#if NET6_0_OR_GREATER
    /// <summary>
    /// ASP.NET Core instrumentation.
    /// </summary>
    AspNetCore = 6
#endif
}

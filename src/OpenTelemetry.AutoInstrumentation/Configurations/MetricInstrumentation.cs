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
    /// <summary>
    /// ASP.NET instrumentation.
    /// </summary>
    AspNet,

    /// <summary>
    /// HttpClient instrumentation.
    /// </summary>
    HttpClient,

    /// <summary>
    /// .NET Runtime Metrics instrumentation.
    /// </summary>
    NetRuntime,

    /// <summary>
    /// Process instrumentation.
    /// </summary>
    Process,

    /// <summary>
    /// NServiceBus instrumentation.
    /// </summary>
    NServiceBus
}

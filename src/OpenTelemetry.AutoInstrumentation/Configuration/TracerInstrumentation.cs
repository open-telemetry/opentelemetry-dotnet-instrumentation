// <copyright file="TracerInstrumentation.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// Enum representing supported instrumentations.
/// </summary>
public enum TracerInstrumentation
{
    /// <summary>
    /// HttpClient instrumentation.
    /// </summary>
    HttpClient = 0,

    /// <summary>
    /// ASP.NET instrumentation.
    /// </summary>
    AspNet = 1,

    /// <summary>
    /// SqlClient instrumentation.
    /// </summary>
    SqlClient = 2,

    /// <summary>
    /// GraphQL instrumentation.
    /// </summary>
    GraphQL = 3,

#if NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// MongoDB instrumentation.
    /// </summary>
    MongoDB = 4,
#endif

    /// <summary>
    /// Npgsql instrumentation.
    /// </summary>
    Npgsql = 5,

#if NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// MySqlData instrumentation.
    /// </summary>
    MySqlData = 6,

    /// <summary>
    /// StackExchangeRedis instrumentation.
    /// </summary>
    StackExchangeRedis = 7,
#endif

    /// <summary>
    /// GrpcNetClient instrumentation.
    /// </summary>
    GrpcNetClient = 8,

    /// <summary>
    /// WCF instrumentation.
    /// </summary>
    Wcf = 9
}

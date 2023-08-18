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

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Enum representing supported instrumentations.
/// </summary>
internal enum TracerInstrumentation
{
    /// <summary>
    /// HttpClient instrumentation.
    /// </summary>
    HttpClient = 0,

#if NETFRAMEWORK
    /// <summary>
    /// ASP.NET instrumentation.
    /// </summary>
    AspNet = 1,
#endif

    /// <summary>
    /// SqlClient instrumentation.
    /// </summary>
    SqlClient = 2,

#if NET6_0_OR_GREATER
    /// <summary>
    /// GraphQL instrumentation.
    /// </summary>
    GraphQL = 3,
#endif

    /// <summary>
    /// MongoDB instrumentation.
    /// </summary>
    MongoDB = 4,

    /// <summary>
    /// Npgsql instrumentation.
    /// </summary>
    Npgsql = 5,

#if NET6_0_OR_GREATER
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
#if NETFRAMEWORK
    /// <summary>
    /// WCF instrumentation.
    /// </summary>
    WcfService = 9,
#endif

#if NET6_0_OR_GREATER
    /// <summary>
    /// MassTransit instrumentation.
    /// </summary>
    MassTransit = 10,
#endif

    /// <summary>
    /// NServiceBus instrumentation.
    /// </summary>
    NServiceBus = 11,

    /// <summary>
    /// Elasticsearch instrumentation.
    /// </summary>
    Elasticsearch = 12,

    /// <summary>
    /// Quartz instrumentation.
    /// </summary>
    Quartz = 13,

#if NET6_0_OR_GREATER
    /// <summary>
    /// Entity Framework Core instrumentation.
    /// </summary>
    EntityFrameworkCore = 14,

    /// <summary>
    /// ASP.NET Core instrumentation.
    /// </summary>
    AspNetCore = 15,
#endif
#if NETFRAMEWORK
    /// <summary>
    /// WcfClient instrumentation.
    /// </summary>
    WcfClient = 16,
#endif

    /// <summary>
    /// MySqlConnector instrumentation.
    /// </summary>
    MySqlConnector = 17,

    /// <summary>
    /// Azure SDK instrumentation.
    /// </summary>
    Azure = 18
}

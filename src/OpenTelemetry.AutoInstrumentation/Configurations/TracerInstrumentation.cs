// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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

#if NET
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

#if NET
    /// <summary>
    /// MySqlData instrumentation.
    /// </summary>
    MySqlData = 6,
#endif

    /// <summary>
    /// StackExchangeRedis instrumentation.
    /// </summary>
    StackExchangeRedis = 7,

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

#if NET
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

#if NET
    /// <summary>
    /// Entity Framework Core instrumentation.
    /// </summary>
    EntityFrameworkCore = 14,

    /// <summary>
    /// ASP.NET Core instrumentation.
    /// </summary>
    AspNetCore = 15,
#endif

    /// <summary>
    /// WcfClient instrumentation.
    /// </summary>
    WcfClient = 16,

    /// <summary>
    /// MySqlConnector instrumentation.
    /// </summary>
    MySqlConnector = 17,

    /// <summary>
    /// Azure SDK instrumentation.
    /// </summary>
    Azure = 18,

    /// <summary>
    /// Elastic.Transport instrumentation.
    /// </summary>
    ElasticTransport = 19,

    /// <summary>
    /// Kafka client instrumentation
    /// </summary>
    Kafka = 20,

    /// <summary>
    /// Oracle Managed Data Access (Core) instrumentation
    /// </summary>
    OracleMda = 21,

    /// <summary>
    /// RabbitMQ client instrumentation
    /// </summary>
    RabbitMq = 22,

#if NET
    /// <summary>
    /// CoreWCF instrumentation.
    /// </summary>
    WcfCore = 23
#endif
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class DotNetTraces
{
    /// <summary>
    /// Gets or sets the list of additional `System.Diagnostics.ActivitySource` names to be added to the tracer at the startup.
    /// </summary>
    [YamlMember(Alias = "additional_sources")]
    public List<string>? AdditionalSources { get; set; }

    /// <summary>
    /// Gets or sets additional`System.Diagnostics.ActivitySource` names list to be added to the tracer at the startup.
    /// </summary>
    [YamlMember(Alias = "additional_sources_list")]
    public string? AdditionalSourcesList { get; set; }

    /// <summary>
    /// Gets or sets the list of additional `System.Diagnostics.Activity` names to be added to the tracer at the startup.
    /// </summary>
    [YamlMember(Alias = "additional_legacy_sources")]
    public List<string>? AdditionalLegacySources { get; set; }

    /// <summary>
    /// Gets or sets additional `System.Diagnostics.Activity` names list to be added to the tracer at the startup.
    /// </summary>
    [YamlMember(Alias = "additional_legacy_sources_list")]
    public string? AdditionalLegacySourcesList { get; set; }

#if NETFRAMEWORK
    /// <summary>
    /// Gets or sets the ASP.NET traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "aspnet")]
    public CaptureHeadersConfiguration? AspNet { get; set; }

    /// <summary>
    /// Gets or sets the WCF service traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "wcfservice")]
    public object? WcfService { get; set; }
#endif

#if NET
    /// <summary>
    /// Gets or sets the ASP.NET Core traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "aspnetcore")]
    public CaptureHeadersConfiguration? AspNetCore { get; set; }
#endif

    /// <summary>
    /// Gets or sets the StackExchange.Redis traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "stackexchangeredis")]
    public object? StackExchangeRedis { get; set; }

#if NET
    /// <summary>
    /// Gets or sets the Entity Framework Core traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "entityframeworkcore")]
    public object? EntityFrameworkCore { get; set; }

    /// <summary>
    /// Gets or sets the GraphQL traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "graphql")]
    public GraphQLSetDocumentConfiguration? GraphQL { get; set; }

    /// <summary>
    /// Gets or sets the MassTransit traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "masstransit")]
    public object? MassTransit { get; set; }

    /// <summary>
    /// Gets or sets the MySqlData traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "mysqldata")]
    public object? MySqlData { get; set; }
#endif

    /// <summary>
    /// Gets or sets the Azure traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "azure")]
    public object? Azure { get; set; }

    /// <summary>
    /// Gets or sets the Elasticsearch traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "elasticsearch")]
    public object? Elasticsearch { get; set; }

    /// <summary>
    /// Gets or sets the ElasticTransport traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "elastictransport")]
    public object? ElasticTransport { get; set; }

    /// <summary>
    /// Gets or sets the Grpc.Net.Client traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "grpcnetclient")]
    public CaptureMetadataConfiguration? GrpcNetClient { get; set; }

    /// <summary>
    /// Gets or sets the HttpClient traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "httpclient")]
    public CaptureHeadersConfiguration? HttpClient { get; set; }

    /// <summary>
    /// Gets or sets the Kafka traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "kafka")]
    public object? Kafka { get; set; }

    /// <summary>
    /// Gets or sets the MongoDB traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "mongodb")]
    public object? MongoDb { get; set; }

    /// <summary>
    /// Gets or sets the MySqlConnector traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "mysqlconnector")]
    public object? MySqlConnector { get; set; }

    /// <summary>
    /// Gets or sets the Npgsql traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "npgsql")]
    public object? Npgsql { get; set; }

    /// <summary>
    /// Gets or sets the NServiceBus traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "nservicebus")]
    public object? NServiceBus { get; set; }

    /// <summary>
    /// Gets or sets the Oracle MDA traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "oraclemda")]
    public SetDbStatementForTextConfuguration? OracleMda { get; set; }

    /// <summary>
    /// Gets or sets the RabbitMQ traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "rabbitmq")]
    public object? RabbitMq { get; set; }

    /// <summary>
    /// Gets or sets the Quartz traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "quartz")]
    public object? Quartz { get; set; }

    /// <summary>
    /// Gets or sets the SqlClient traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "sqlclient")]
    public SqlClientConfiguration? SqlClient { get; set; }

    /// <summary>
    /// Gets or sets the WCF client traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "wcfclient")]
    public object? WcfClient { get; set; }

#if NET
    /// <summary>
    /// Gets or sets the CoreWCF traces instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "wcfcore")]
    public object? WcfCore { get; set; }
#endif

    /// <summary>
    /// Returns the list of enabled traces instrumentations.
    /// </summary>
    public IReadOnlyList<TracerInstrumentation> GetEnabledInstrumentations()
    {
        var enabled = new List<TracerInstrumentation>();
        var properties = typeof(DotNetTraces).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var prop in properties)
        {
            var value = prop.GetValue(this);
            if (value != null)
            {
                if (Enum.TryParse<TracerInstrumentation>(prop.Name, out var instrumentation))
                {
                    enabled.Add(instrumentation);
                }
            }
        }

        return enabled;
    }
}

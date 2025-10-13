// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

public class ParserInstrumentationTests
{
    [Fact]
    public void Parse_Instrumentation_ShouldPopulateModelCorrectly()
    {
        var config = YamlParser.ParseYaml("Configurations/FileBased/Files/TestInstrumentationFile.yaml");

        Assert.NotNull(config);
        Assert.NotNull(config.InstrumentationDevelopment);
        Assert.NotNull(config.InstrumentationDevelopment.DotNet);

#if NET
        string[] expectedTraces = [
                    "aspnetcore", "azure", "elasticsearch", "elastictransport",
                    "entityframeworkcore", "graphql", "grpcnetclient", "httpclient",
                    "kafka", "masstransit", "mongodb", "mysqlconnector",
                    "mysqldata", "npgsql", "nservicebus", "oraclemda", "rabbitmq",
                    "quartz", "sqlclient", "stackexchangeredis", "wcfclient"
                ];
#endif
#if NETFRAMEWORK
        string[] expectedTraces = [
                    "aspnet", "azure", "elasticsearch", "elastictransport",
                    "grpcnetclient", "httpclient", "kafka", "mongodb",
                    "mysqlconnector", "npgsql", "nservicebus", "oraclemda",
                    "rabbitmq", "quartz", "sqlclient", "wcfclient", "wcfservice"
                ];
#endif

        var traces = config.InstrumentationDevelopment.DotNet.Traces;
        Assert.NotNull(traces);

        foreach (var alias in expectedTraces)
        {
            FileBasedTestHelper.AssertAliasPropertyExists(traces, alias);
        }

#if NET
        string[] expectedMetrics =
        [
            "aspnetcore", "httpclient", "netruntime",
            "nservicebus", "process", "sqlclient"
        ];
#endif
#if NETFRAMEWORK
        string[] expectedMetrics =
        [
            "aspnet", "httpclient", "netruntime",
            "nservicebus", "process", "sqlclient"
        ];
#endif

        var metrics = config.InstrumentationDevelopment.DotNet.Metrics;
        Assert.NotNull(metrics);

        foreach (var alias in expectedMetrics)
        {
            FileBasedTestHelper.AssertAliasPropertyExists(metrics, alias);
        }

        string[] expectedLogs = ["ilogger", "log4net"];

        var logs = config.InstrumentationDevelopment.DotNet.Logs;
        Assert.NotNull(logs);

        foreach (var alias in expectedLogs)
        {
            FileBasedTestHelper.AssertAliasPropertyExists(logs, alias);
        }
    }

    [Fact]
    public void Parse_InstrumentationConfiguration_ShouldPopulateModelCorrectly()
    {
        var config = YamlParser.ParseYaml("Configurations/FileBased/Files/TestInstrumentationConfigurationFile.yaml");

        Assert.NotNull(config);
        Assert.NotNull(config.InstrumentationDevelopment);
        Assert.NotNull(config.InstrumentationDevelopment.DotNet);

#if NET
        string[] expectedTraces = [
                    "aspnetcore", "entityframeworkcore", "graphql", "grpcnetclient", "httpclient", "oraclemda"
                ];
#endif
#if NETFRAMEWORK
        string[] expectedTraces = [
                    "aspnet", "httpclient", "oraclemda", "grpcnetclient"
                ];
#endif

        var traces = config.InstrumentationDevelopment.DotNet.Traces;
        Assert.NotNull(traces);

        foreach (var alias in expectedTraces)
        {
            FileBasedTestHelper.AssertAliasPropertyExists(traces, alias);
        }

        Assert.True(traces.OracleMda!.SetDbStatementForText);
        Assert.Equal("X-Key", traces.HttpClient!.CaptureRequestHeaders);
        Assert.Equal("X-Key", traces.HttpClient!.CaptureResponseHeaders);

        Assert.Equal("X-Key", traces.GrpcNetClient!.CaptureRequestMetadata);
        Assert.Equal("X-Key", traces.GrpcNetClient!.CaptureResponseMetadata);
#if NET
        Assert.True(traces.GraphQL!.SetDocument);
        Assert.True(traces.EntityFrameworkCore!.SetDbStatementForText);

        Assert.Equal("X-Key", traces.AspNetCore!.CaptureRequestHeaders);
        Assert.Equal("X-Key", traces.AspNetCore!.CaptureResponseHeaders);

#endif
#if NETFRAMEWORK
        Assert.Equal("X-Key", traces.AspNet!.CaptureRequestHeaders);
        Assert.Equal("X-Key", traces.AspNet!.CaptureResponseHeaders);
#endif
    }
}

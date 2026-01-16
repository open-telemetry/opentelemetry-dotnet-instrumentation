// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

[Collection("Non-Parallel Collection")]
public class ParserInstrumentationTests
{
    [Fact]
    public void Parse_Instrumentation_ShouldPopulateModelCorrectly()
    {
        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestInstrumentationFile.yaml");

        Assert.NotNull(config);
        Assert.NotNull(config.InstrumentationDevelopment);
        Assert.NotNull(config.InstrumentationDevelopment.DotNet);

        string[] expectedTraces =
        [
#if NETFRAMEWORK
            "aspnet",
#else
            "aspnetcore",
#endif
            "azure",
            "elasticsearch",
            "elastictransport",
#if NET
            "entityframeworkcore",
            "graphql",
#endif
            "grpcnetclient",
            "httpclient",
            "kafka",
#if NET
            "masstransit",
#endif
            "mongodb",
            "mysqlconnector",
#if NET
            "mysqldata",
#endif
            "npgsql",
            "nservicebus",
            "oraclemda",
            "rabbitmq",
            "quartz",
            "sqlclient",
            "stackexchangeredis",
            "wcfclient",
#if NET
            "wcfcore"
#endif
#if NETFRAMEWORK
            "wcfservice",
#endif
        ];

        var traces = config.InstrumentationDevelopment.DotNet.Traces;
        Assert.NotNull(traces);

        const int countOfNonTracesProperties = 4;
        FileBasedTestHelper.AssertCountOfAliasProperties(traces, expectedTraces.Length + countOfNonTracesProperties);

        foreach (var alias in expectedTraces)
        {
            FileBasedTestHelper.AssertAliasPropertyExists(traces, alias);
        }

        Assert.NotNull(traces.AdditionalSources);
        Assert.Contains("Some.Additional.Source1", traces.AdditionalSources);
        Assert.Contains("Some.Additional.Source2", traces.AdditionalSources);

        Assert.NotNull(traces.AdditionalLegacySources);
        Assert.Contains("Some.Additional.Legacy.Source1", traces.AdditionalLegacySources);
        Assert.Contains("Some.Additional.Legacy.Source2", traces.AdditionalLegacySources);

        string[] expectedMetrics =
        [
#if NETFRAMEWORK
            "aspnet",
#else
            "aspnetcore",
#endif
            "httpclient",
            "netruntime",
            "nservicebus",
            "process",
            "sqlclient",
        ];

        var metrics = config.InstrumentationDevelopment.DotNet.Metrics;
        Assert.NotNull(metrics);

        foreach (var alias in expectedMetrics)
        {
            FileBasedTestHelper.AssertAliasPropertyExists(metrics, alias);
        }

        Assert.NotNull(metrics.AdditionalSources);
        Assert.Contains("Some.Additional.Source1", metrics.AdditionalSources);
        Assert.Contains("Some.Additional.Source2", metrics.AdditionalSources);

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
        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestInstrumentationConfigurationFile.yaml");

        Assert.NotNull(config);
        Assert.NotNull(config.InstrumentationDevelopment);
        Assert.NotNull(config.InstrumentationDevelopment.DotNet);

        string[] expectedTraces =
        [
#if NETFRAMEWORK
            "aspnet",
#else
            "aspnetcore",
            "graphql",
#endif
            "grpcnetclient",
            "httpclient",
            "oraclemda",
            "sqlclient",
        ];

        var traces = config.InstrumentationDevelopment.DotNet.Traces;
        Assert.NotNull(traces);

        foreach (var alias in expectedTraces)
        {
            FileBasedTestHelper.AssertAliasPropertyExists(traces, alias);
        }

        var logs = config.InstrumentationDevelopment.DotNet.Logs;
        Assert.NotNull(logs);
        Assert.NotNull(logs.Log4Net);

        Assert.True(traces.OracleMda!.SetDbStatementForText);
        Assert.Equal("X-Key,X-Custom-Header,X-Header-Example", traces.HttpClient!.CaptureRequestHeaders);
        Assert.Equal("X-Key,X-Custom-Header,X-Header-Example", traces.HttpClient!.CaptureResponseHeaders);

        Assert.Equal("X-Key,X-Custom-Header,X-Header-Example", traces.GrpcNetClient!.CaptureRequestMetadata);
        Assert.Equal("X-Key,X-Custom-Header,X-Header-Example", traces.GrpcNetClient!.CaptureResponseMetadata);
#if NET
        Assert.True(traces.GraphQL!.SetDocument);

        Assert.Equal("X-Key,X-Custom-Header,X-Header-Example", traces.AspNetCore!.CaptureRequestHeaders);
        Assert.Equal("X-Key,X-Custom-Header,X-Header-Example", traces.AspNetCore!.CaptureResponseHeaders);

#endif
#if NETFRAMEWORK
        Assert.Equal("X-Key,X-Custom-Header,X-Header-Example", traces.AspNet!.CaptureRequestHeaders);
        Assert.Equal("X-Key,X-Custom-Header,X-Header-Example", traces.AspNet!.CaptureResponseHeaders);
        Assert.True(traces.SqlClient!.NetFxIlRewriteEnabled);
#endif

        Assert.True(logs.Log4Net.BridgeEnabled);
    }

    [Fact]
    public void Parse_EnvVarYaml_ShouldPopulateModelCompletely()
    {
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "Some.Additional.Source1,Some.Additional.Source2");
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES", "Some.Additional.Legacy.Source1,Some.Additional.Legacy.Source2");
        Environment.SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "Some.Additional.Source1,Some.Additional.Source2");

        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestInstrumentationFileEnvVars.yaml");

        Assert.NotNull(config);
        Assert.NotNull(config.InstrumentationDevelopment);
        Assert.NotNull(config.InstrumentationDevelopment.DotNet);

        var traces = config.InstrumentationDevelopment.DotNet.Traces;
        Assert.NotNull(traces);
        Assert.NotNull(traces.AdditionalSourcesList);
        Assert.NotNull(traces.AdditionalLegacySourcesList);
        Assert.Equal("Some.Additional.Source1,Some.Additional.Source2", traces.AdditionalSourcesList);
        Assert.Equal("Some.Additional.Legacy.Source1,Some.Additional.Legacy.Source2", traces.AdditionalLegacySourcesList);

        var metrics = config.InstrumentationDevelopment.DotNet.Metrics;
        Assert.NotNull(metrics);
        Assert.NotNull(metrics.AdditionalSourcesList);
        Assert.Equal("Some.Additional.Source1,Some.Additional.Source2", metrics.AdditionalSourcesList);
    }
}

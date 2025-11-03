// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

[Collection("Non-Parallel Collection")]
public class ParserPropagatorTests
{
    [Fact]
    public void Parse_FullConfigYaml_ShouldPopulateModelCorrectly()
    {
        var config = YamlParser.ParseYaml("Configurations/FileBased/Files/TestPropagatorFile.yaml");

        Assert.NotNull(config);
        Assert.NotNull(config.Propagator);
        Assert.NotNull(config.Propagator.Composite);
        var composite = config.Propagator.Composite;
        Assert.Null(config.Propagator.CompositeList);

        string[] expectedPropagators = [
            "tracecontext", "baggage", "b3", "b3multi",
                ];

        foreach (var expected in expectedPropagators)
        {
            Assert.Contains(expected, composite.Keys);
        }
    }

    [Fact]
    public void Parse_EnvVarYaml_ShouldPopulateModelCompletely()
    {
        Environment.SetEnvironmentVariable("OTEL_PROPAGATORS", "tracecontext,baggage,b3,b3multi");

        var config = YamlParser.ParseYaml("Configurations/FileBased/Files/TestPropagatorFileEnvVars.yaml");

        Assert.NotNull(config);
        Assert.NotNull(config.Propagator);
        Assert.NotNull(config.Propagator.CompositeList);
        Assert.Null(config.Propagator.Composite);
        Assert.Equal("tracecontext,baggage,b3,b3multi", config.Propagator.CompositeList);
    }
}

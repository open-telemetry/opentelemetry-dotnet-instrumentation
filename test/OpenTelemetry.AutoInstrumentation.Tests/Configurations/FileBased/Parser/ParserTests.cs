// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

[Collection("Non-Parallel Collection")]
public class ParserTests
{
    [Fact]
    public void Parse_FullConfigYaml_ShouldPopulateModelCorrectly()
    {
        var config = YamlParser.ParseYaml("Configurations/FileBased/Files/TestFile.yaml");

        Assert.NotNull(config);

        Assert.Equal("1.0-rc.1", config.FileFormat);

        Assert.NotNull(config.Resource);
        Assert.NotNull(config.Resource.Attributes);
        Assert.Single(config.Resource.Attributes);

        var serviceAttr = config.Resource.Attributes.First();
        Assert.Equal("service.name", serviceAttr.Name);
        Assert.Equal("unknown_service", serviceAttr.Value);

        Assert.NotNull(config.Resource.AttributesList);
        Assert.Empty(config.Resource.AttributesList);
        Assert.NotNull(config.Resource.DetectionDevelopment);

#if NET
        string[] expectedDetecors = [
            "azureappservice", "container", "host", "operatingsystem", "process", "processruntime"
                ];
#endif
#if NETFRAMEWORK
        string[] expectedDetecors = [
            "azureappservice", "host", "operatingsystem", "process", "processruntime"
                ];
#endif

        var detectors = config.Resource.DetectionDevelopment.Detectors;
        Assert.NotNull(detectors);

        foreach (var alias in expectedDetecors)
        {
            FileBasedTestHelper.AssertAliasPropertyExists(detectors, alias);
        }
    }

    [Fact]
    public void Parse_EnvVarYaml_ShouldPopulateModelCompletely()
    {
        Environment.SetEnvironmentVariable("OTEL_SDK_DISABLED", "true");
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "my‑service");
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "key=value");

        var config = YamlParser.ParseYaml("Configurations/FileBased/Files/TestFileEnvVars.yaml");

        Assert.Equal("1.0-rc.1", config.FileFormat);
        var serviceAttr = config.Resource?.Attributes?.First(a => a.Name == "service.name");
        Assert.NotNull(serviceAttr);
        Assert.Equal("my‑service", serviceAttr.Value);
    }
}

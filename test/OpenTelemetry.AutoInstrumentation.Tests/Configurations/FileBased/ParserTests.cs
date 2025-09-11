// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

[Collection("Non-Parallel Collection")]
public class ParserTests
{
    [Fact]
    public void Parse_FullConfigYaml_ShouldPopulateModelCorrectly()
    {
        var config = Parser.ParseYaml("Configurations/FileBased/Files/TestFile.yaml");

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
    }

    [Fact]
    public void Parse_EnvVarYaml_ShouldPopulateModelCompletely()
    {
        Environment.SetEnvironmentVariable("OTEL_SDK_DISABLED", "true");
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "my‑service");
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "key=value");

        var config = Parser.ParseYaml("Configurations/FileBased/Files/TestFileEnvVars.yaml");

        Assert.Equal("1.0-rc.1", config.FileFormat);
        var serviceAttr = config.Resource?.Attributes?.First(a => a.Name == "service.name");
        Assert.NotNull(serviceAttr);
        Assert.Equal("my‑service", serviceAttr.Value);
    }
}

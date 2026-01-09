// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

[Collection("Non-Parallel Collection")]
public class ParserResourceTests
{
    [Fact]
    public void Parse_FullConfigYaml_ShouldPopulateModelCorrectly()
    {
        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestResourceFile.yaml");

        Assert.NotNull(config);

        Assert.Equal("1.0-rc.1", config.FileFormat);

        Assert.NotNull(config.Resource);
        Assert.NotNull(config.Resource.Attributes);

        List<YamlAttribute> expectedAttributes = [
            new() { Name = "service.name", Value = "unknown_service" },
            new() { Name = "attribute_key_string", Value = "string_value", Type = "string" },
            new() { Name = "attribute_key_string_not_supported", Value = new[] { "string_value" }, Type = "string" },
            new() { Name = "attribute_key_bool", Value = "true", Type = "bool" },
            new() { Name = "attribute_key_bool_not_supported", Value = new[] { "true" }, Type = "bool" },
            new() { Name = "attribute_key_int", Value = "12345", Type = "int" },
            new() { Name = "attribute_key_int_not_supported", Value = new[] { "12345" }, Type = "int" },
            new() { Name = "attribute_key_double", Value = "123.45", Type = "double" },
            new() { Name = "attribute_key_double_not_supported", Value = new[] { "123.45" }, Type = "double" },
            new() { Name = "attribute_key_string_array", Value = new[] { "value1", "value2", "value3" }, Type = "string_array" },
            new() { Name = "attribute_key_string_array_not_supported", Value = new object[] { "value1", new object[] { "value2" }, "value3" }, Type = "string_array" },
            new() { Name = "attribute_key_bool_array", Value = new[] { true, false, true }, Type = "bool_array" },
            new() { Name = "attribute_key_bool_array_not_supported", Value = new object[] { true, new[] { false }, true }, Type = "bool_array" },
            new() { Name = "attribute_key_int_array", Value = new[] { "123", "456", "789" }, Type = "int_array" },
            new() { Name = "attribute_key_int_array_not_supported", Value = new object[] { new[] { "123" }, "456", "789" }, Type = "int_array" },
            new() { Name = "attribute_key_double_array", Value = new object[] { "123.45", "678.90" }, Type = "double_array" },
            new() { Name = "attribute_key_double_array_not_supported", Value = new object[] { "123.45", new[] { "678.90" } }, Type = "double_array" },
            new() { Name = "attribute_key_non_supported_type", Value = "non_supported_value", Type = "non_supported_type" },
        ];

        Assert.Equivalent(expectedAttributes, config.Resource.Attributes);

        var tagList = config.Resource.ParseAttributes();

        List<KeyValuePair<string, object>> expectedTagList =
        [
            new("service.name", "unknown_service"),
            new("attribute_key_string", "string_value"),
            new("attribute_key_bool", true),
            new("attribute_key_int", 12345L),
            new("attribute_key_double", 123.45),
            new("attribute_key_string_array", (string[])["value1", "value2", "value3"]),
            new("attribute_key_bool_array", (bool[])[true, false, true]),
            new("attribute_key_int_array", (long[])[123, 456, 789]),
            new("attribute_key_double_array", (double[])[123.45, 678.90]),
        ];

        Assert.Equal(expectedTagList, tagList);

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

        var config = YamlParser.ParseYaml<YamlConfiguration>("Configurations/FileBased/Files/TestResourceFileEnvVars.yaml");

        Assert.Equal("1.0-rc.1", config.FileFormat);
        var serviceAttr = config.Resource?.Attributes?.First(a => a.Name == "service.name");
        Assert.NotNull(serviceAttr);
        Assert.Equal("my‑service", serviceAttr.Value);
    }
}

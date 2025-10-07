// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class NoCodeConfigurationTests
{
    [Fact]
    public void NoCodeConfigurationCanBeParsed()
    {
        var config = YamlParser.ParseYaml("Configurations/FileBased/Files/NoCodeFile.yaml");

        Assert.NotNull(config);
        Assert.Equal("1.0-rc.1", config.FileFormat);

        Assert.NotNull(config.NoCode);
        Assert.NotNull(config.NoCode.Targets);
        var noCodeEntry = Assert.Single(config.NoCode.Targets);
        Assert.NotNull(noCodeEntry.Target);
        var target = noCodeEntry.Target;
        Assert.NotNull(target.Assembly);
        Assert.Equal("TargetAssembly", target.Assembly.Name);
        Assert.Equal("TargetNamespace.TargetType", target.Type);
        Assert.Equal("TargetMethod", target.Method);

        Assert.NotNull(target.Signature);
        Assert.Equal("ReturnType", target.Signature.ReturnType);
        Assert.NotNull(target.Signature.ParameterTypes);
        Assert.Equal(["ParameterType1", "ParameterType2"], target.Signature.ParameterTypes);

        Assert.NotNull(noCodeEntry.Span);
        Assert.Equal("SpanName", noCodeEntry.Span.Name);
        Assert.Equal("server", noCodeEntry.Span.Kind);

        Assert.NotNull(noCodeEntry.Span.Attributes);

        List<NoCodeAttribute> expectedAttributes = [
            new() { Name = "attribute_key_string", Value = "string_value", Type = "string" },
            new() { Name = "attribute_key_bool", Value = "true", Type = "bool" },
            new() { Name = "attribute_key_int", Value = "12345", Type = "int" },
            new() { Name = "attribute_key_double", Value = "123.45", Type = "double" },
            new() { Name = "attribute_key_string_array", Value = new[] { "value1", "value2", "value3" }, Type = "string_array" },
            new() { Name = "attribute_key_string_array_not_supported", Value = new object[] { "value1", new object[] { "value2" }, "value3" }, Type = "string_array" },
            new() { Name = "attribute_key_bool_array", Value = new[] { true, false, true }, Type = "bool_array" },
            new() { Name = "attribute_key_bool_array_not_supported", Value = new object[] { true, new[] { false }, true }, Type = "bool_array" },
        ];

        Assert.Equivalent(expectedAttributes, noCodeEntry.Span.Attributes);

        var tagList = noCodeEntry.Span.ParseAttributes();

        TagList expectedTagList = default;
        expectedTagList.Add("attribute_key_string", "string_value");
        expectedTagList.Add("attribute_key_bool", true);
        expectedTagList.Add("attribute_key_int", 12345L);
        expectedTagList.Add("attribute_key_double", 123.45);
        expectedTagList.Add("attribute_key_string_array", new[] { "value1", "value2", "value3" });
        expectedTagList.Add("attribute_key_bool_array", new[] { true, false, true });

        Assert.Equal(expectedTagList, tagList);
    }
}

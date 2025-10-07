// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
    }
}

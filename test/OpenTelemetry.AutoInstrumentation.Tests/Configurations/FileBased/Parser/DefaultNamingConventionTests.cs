// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased.Parser;

public class DefaultNamingConventionTests
{
    [Fact]
    public void Test_UnderScore()
    {
        var config = YamlParser.ParseYaml<DefaultNamingConvention>("Configurations/FileBased/Files/DefaultNamingConvention.yaml");

        Assert.True(config.UnderScore);
        Assert.True(config.UnderScoreDevelopment);
    }

#pragma warning disable CA1812 //  Avoid uninstantiated internal classes. Used in tests by Yaml deserializer.
    internal sealed class DefaultNamingConvention
#pragma warning restore CA1812 //  Avoid uninstantiated internal classes. Used in tests by Yaml deserializer.
    {
        public bool UnderScore { get; set; }

        public bool UnderScoreDevelopment { get; set; }
    }
}

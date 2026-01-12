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

    public class DefaultNamingConvention
    {
        public bool UnderScore { get; set; }

        public bool UnderScoreDevelopment { get; set; }
    }
}

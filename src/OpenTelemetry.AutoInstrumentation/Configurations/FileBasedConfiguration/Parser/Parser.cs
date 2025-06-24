// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;

internal static class Parser
{
    public static Conf ParseYaml(string filePath)
    {
        var deserializer = new DeserializerBuilder()
            // .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(new EnvVarTypeConverter())
            .Build();

        var yaml = File.ReadAllText(filePath);
        var config = deserializer.Deserialize<Conf>(yaml);
        return config;
    }
}

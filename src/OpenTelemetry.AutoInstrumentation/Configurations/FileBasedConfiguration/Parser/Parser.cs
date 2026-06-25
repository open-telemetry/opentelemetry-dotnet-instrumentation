// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;
using Vendors.YamlDotNet.Serialization.NodeDeserializers;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;

internal static class Parser
{
    public static T ParseYaml<T>(string filePath)
    {
        var yaml = File.ReadAllText(filePath);
        return ParseYamlContent<T>(yaml);
    }

    public static T ParseYamlContent<T>(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNodeDeserializer(existing => new ConditionalDeserializer(existing), s => s.InsteadOf<NullNodeDeserializer>())
            .WithTypeConverter(new EnvVarTypeConverter())
            .WithNamingConvention(OtelDefaultNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var config = deserializer.Deserialize<T>(yaml);
        return config;
    }
}
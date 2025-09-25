// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;
using Vendors.YamlDotNet.Serialization.NodeDeserializers;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;

internal static class Parser
{
    public static YamlConfiguration ParseYaml(string filePath)
    {
        var deserializer = new DeserializerBuilder()
            .WithNodeDeserializer(existing => new ConditionalDeserializer(existing), s => s.InsteadOf<NullNodeDeserializer>())
            .WithTypeConverter(new EnvVarTypeConverter())
            .IgnoreUnmatchedProperties()
            .Build();

        var yaml = File.ReadAllText(filePath);
        var config = deserializer.Deserialize<YamlConfiguration>(yaml);
        return config;
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedResourceSettingsTests
{
    [Fact]
    public void LoadFile_MergesBaseAndCustomResourceAttributes()
    {
        var resource = new ResourceConfiguration
        {
            AttributesList = "custom1=value1,custom2=value2",
            Attributes =
            [
                new() { Name = "custom1", Value = "OverridesAttributesList" },
                new() { Name = "custom3", Value = "value3" }
            ]
        };

        var conf = new YamlConfiguration { Resource = resource };
        var settings = new ResourceSettings();

        settings.LoadFile(conf);

        var result = settings.Resources.ToDictionary(kv => kv.Key, kv => kv.Value);

        Assert.Equal("OverridesAttributesList", result["custom1"]);
        Assert.Equal("value2", result["custom2"]);
        Assert.Equal("value3", result["custom3"]);
    }

    [Fact]
    public void LoadFile_AttributesListOnly_IsParsedCorrectly()
    {
        var resource = new ResourceConfiguration
        {
            AttributesList = "key1=value1,key2=value2"
        };

        var conf = new YamlConfiguration { Resource = resource };
        var settings = new ResourceSettings();

        settings.LoadFile(conf);

        var result = settings.Resources.ToDictionary(kv => kv.Key, kv => kv.Value);

        Assert.Equal("value1", result["key1"]);
        Assert.Equal("value2", result["key2"]);
    }

    [Fact]
    public void LoadFile_AttributesOnly_IsParsedCorrectly()
    {
        var resource = new ResourceConfiguration
        {
            Attributes =
            [
                new() { Name = "a", Value = "1" },
                new() { Name = "b", Value = "2" }
            ]
        };

        var conf = new YamlConfiguration { Resource = resource };
        var settings = new ResourceSettings();

        settings.LoadFile(conf);

        var result = settings.Resources.ToDictionary(kv => kv.Key, kv => kv.Value);

        Assert.Equal("1", result["a"]);
        Assert.Equal("2", result["b"]);
    }

    [Fact]
    public void LoadFile_AttributesListOverwrittenByAttributes()
    {
        var resource = new ResourceConfiguration
        {
            AttributesList = "key1=fromList",
            Attributes =
            [
                new() { Name = "key1", Value = "fromAttributes" }
            ]
        };

        var conf = new YamlConfiguration { Resource = resource };
        var settings = new ResourceSettings();

        settings.LoadFile(conf);

        var result = settings.Resources.ToDictionary(kv => kv.Key, kv => kv.Value);

        Assert.Equal("fromAttributes", result["key1"]);
    }

    [Fact]
    public void GetEnabledResourceDetector_ReturnsCorrectEnabledDetectors()
    {
        var detectors = new DotNetDetectors
        {
            AzureAppService = new object(),
            Host = new object(),
            OperatingSystem = null,
            Process = new object(),
            ProcessRuntime = null
        };

#if NET
        detectors.Container = new object();
#endif

        var result = detectors.GetEnabledResourceDetectors();

        var expected = new List<ResourceDetector>
        {
            ResourceDetector.AzureAppService,
            ResourceDetector.Host,
            ResourceDetector.Process
        };

#if NET
        expected.Add(ResourceDetector.Container);
#endif

        Assert.Equal(expected.Count, result.Count);
        foreach (var detector in expected)
        {
            Assert.Contains(detector, result);
        }
    }
}

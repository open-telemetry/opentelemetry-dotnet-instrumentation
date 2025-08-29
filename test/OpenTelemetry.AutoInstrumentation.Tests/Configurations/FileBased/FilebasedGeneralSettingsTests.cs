// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

public class FilebasedGeneralSettingsTests
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

        var conf = new Conf { Resource = resource };
        var settings = new GeneralSettings();

        settings.LoadFile(conf);

        var result = settings.Resources.ToDictionary(kv => kv.Key, kv => kv.Value);

        Assert.Equal("OverridesAttributesList", result["custom1"]);
        Assert.Equal("value2", result["custom2"]);
        Assert.Equal("value3", result["custom3"]);

        Assert.True(result.ContainsKey(Constants.DistributionAttributes.TelemetryDistroNameAttributeName));
        Assert.True(result.ContainsKey(Constants.DistributionAttributes.TelemetryDistroVersionAttributeName));
    }

    [Fact]
    public void LoadFile_AttributesListOnly_IsParsedCorrectly()
    {
        var resource = new ResourceConfiguration
        {
            AttributesList = "key1=value1,key2=value2"
        };

        var conf = new Conf { Resource = resource };
        var settings = new GeneralSettings();

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

        var conf = new Conf { Resource = resource };
        var settings = new GeneralSettings();

        settings.LoadFile(conf);

        var result = settings.Resources.ToDictionary(kv => kv.Key, kv => kv.Value);

        Assert.Equal("1", result["a"]);
        Assert.Equal("2", result["b"]);
    }

    [Fact]
    public void LoadFile_BaseAttributes_AreAlwaysIncluded()
    {
        var settings = new GeneralSettings();
        settings.LoadFile(new Conf());

        var result = settings.Resources.ToDictionary(kv => kv.Key, kv => kv.Value);

        Assert.True(result.ContainsKey(Constants.DistributionAttributes.TelemetryDistroNameAttributeName));
        Assert.True(result.ContainsKey(Constants.DistributionAttributes.TelemetryDistroVersionAttributeName));
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

        var conf = new Conf { Resource = resource };
        var settings = new GeneralSettings();

        settings.LoadFile(conf);

        var result = settings.Resources.ToDictionary(kv => kv.Key, kv => kv.Value);

        Assert.Equal("fromAttributes", result["key1"]);
    }

    [Fact]
    public void LoadFile_NullAttributesHandledGracefully()
    {
        var resource = new ResourceConfiguration
        {
            AttributesList = null,
            Attributes = null
        };

        var conf = new Conf { Resource = resource };
        var settings = new GeneralSettings();

        settings.LoadFile(conf);

        var result = settings.Resources.ToDictionary(kv => kv.Key, kv => kv.Value);

        Assert.True(result.ContainsKey(Constants.DistributionAttributes.TelemetryDistroNameAttributeName));
        Assert.True(result.ContainsKey(Constants.DistributionAttributes.TelemetryDistroVersionAttributeName));
    }

    [Fact]
    public void LoadFile_EmptyAttributesList_StillIncludesBaseAttributes()
    {
        var resource = new ResourceConfiguration
        {
            AttributesList = string.Empty
        };

        var conf = new Conf { Resource = resource };
        var settings = new GeneralSettings();

        settings.LoadFile(conf);

        var result = settings.Resources.ToDictionary(kv => kv.Key, kv => kv.Value);

        Assert.True(result.ContainsKey(Constants.DistributionAttributes.TelemetryDistroNameAttributeName));
        Assert.True(result.ContainsKey(Constants.DistributionAttributes.TelemetryDistroVersionAttributeName));
    }

    [Fact]
    public void LoadFile_SetsSetupSdkFromDisabledFlag()
    {
        var conf = new Conf
        {
            Disabled = true
        };

        var settings = new GeneralSettings();
        settings.LoadFile(conf);

        Assert.False(settings.SetupSdk);
    }

    [Fact]
    public void LoadFile_SetsEnabledResourceDetectors()
    {
        var conf = new Conf
        {
            Resource = new ResourceConfiguration
            {
                DetectionDevelopment = new DetectionDevelopment
                {
                    Detectors = new DotNetDetectors
                    {
                        OperatingSystem = new object(),
                        AzureAppService = new object()
                    }
                }
            }
        };

        var settings = new GeneralSettings();
        settings.LoadFile(conf);

        Assert.Contains(ResourceDetector.OperatingSystem, settings.EnabledResourceDetectors);
        Assert.Contains(ResourceDetector.AzureAppService, settings.EnabledResourceDetectors);
    }
}

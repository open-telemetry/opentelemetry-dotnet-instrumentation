// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Tests.Util;
using YamlParser = OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser.Parser;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

[Collection("Non-Parallel Collection")]
public class ResourceConfiguratorTests
{
    private const string CloudPlatform = "cloud.platform";
    private const string CloudProvider = "cloud.provider";
    private const string ServiceName = "service.name";
    private const string ServiceVersion = "service.version";

    [Fact]
    public void AzureContainerAppsDetector_EnabledByEnvironmentVariable()
    {
        // Uses the literal variable name, not the template, to pin the documented key.
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED", "false" },
            { "OTEL_DOTNET_AUTO_AZURECONTAINERAPPS_RESOURCE_DETECTOR_ENABLED", "true" }
        });

        var settings = Settings.FromDefaultSources<ResourceSettings>(false);

        Assert.Equal([ResourceDetector.AzureContainerApps], settings.EnabledDetectors);
    }

    [Fact]
    public void AzureContainerAppsDetector_EnabledByYamlConfiguration()
    {
        // Drives the documented YAML key end to end: text -> parsed model -> enabled detectors.
        const string yaml = """
                            file_format: "1.0-rc.1"
                            resource:
                              detection/development:
                                detectors:
                                  azurecontainerapps:
                            """;

        var configuration = YamlParser.ParseYamlContent<YamlConfiguration>(yaml);
        var settings = new ResourceSettings();

        settings.LoadFile(configuration);

        Assert.Equal([ResourceDetector.AzureContainerApps], settings.EnabledDetectors);
    }

    [Fact]
    public void AzureContainerAppsDetector_AddsAttributes_WhenEnabled()
    {
        // Environment variables set by the Azure Container Apps runtime.
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "CONTAINER_APP_NAME", "test-app" },
            { "CONTAINER_APP_REVISION", "test-revision" }
        });

        var settings = new ResourceSettings
        {
            EnabledDetectors = [ResourceDetector.AzureContainerApps],

            // Isolate the assertions from any ambient OTEL_RESOURCE_ATTRIBUTES.
            EnvironmentalVariablesDetectorEnabled = false
        };

        var resource = ResourceConfigurator.CreateResourceBuilder(settings).Build();

        Assert.Equal("azure", resource.Attributes.FirstOrDefault(a => a.Key == CloudProvider).Value);
        Assert.Equal("azure_container_apps", resource.Attributes.FirstOrDefault(a => a.Key == CloudPlatform).Value);

        // The detector supplies service.name, taking precedence over the fallback.
        Assert.Equal("test-app", resource.Attributes.FirstOrDefault(a => a.Key == ServiceName).Value);
        Assert.Equal("test-revision", resource.Attributes.FirstOrDefault(a => a.Key == ServiceVersion).Value);
    }

    [Fact]
    public void AzureContainerAppsDetector_AddsNoAttributes_WhenNotEnabled()
    {
        using var envScope = new EnvironmentScope(new Dictionary<string, string?>()
        {
            { "CONTAINER_APP_NAME", "test-app" },
            { "CONTAINER_APP_REVISION", "test-revision" }
        });

        var settings = new ResourceSettings
        {
            EnabledDetectors = [],

            // Isolate the assertions from any ambient OTEL_RESOURCE_ATTRIBUTES.
            EnvironmentalVariablesDetectorEnabled = false
        };

        var resource = ResourceConfigurator.CreateResourceBuilder(settings).Build();

        Assert.DoesNotContain(resource.Attributes, a => a.Key == CloudPlatform);
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class DotNetDetectors
{
    /// <summary>
    /// Gets or sets the Azure App Service detector configuration.
    /// </summary>
    [YamlMember(Alias = "azureappservice")]
    public object? AzureAppService { get; set; }

#if NET
    /// <summary>
    /// Gets or sets the container detector configuration.
    /// </summary>
    [YamlMember(Alias = "container")]
    public object? Container { get; set; }
#endif

    /// <summary>
    /// Gets or sets the host detector configuration.
    /// </summary>
    [YamlMember(Alias = "host")]
    public object? Host { get; set; }

    /// <summary>
    /// Gets or sets the operating system detector configuration.
    /// </summary>
    [YamlMember(Alias = "operatingsystem")]
    public object? OperatingSystem { get; set; }

    /// <summary>
    /// Gets or sets the process detector configuration.
    /// </summary>
    [YamlMember(Alias = "process")]
    public object? Process { get; set; }

    /// <summary>
    /// Gets or sets the process runtime detector configuration.
    /// </summary>
    [YamlMember(Alias = "processruntime")]
    public object? ProcessRuntime { get; set; }

    /// <summary>
    /// Returns the list of enabled resource detectors.
    /// </summary>
    public IReadOnlyList<ResourceDetector> GetEnabledResourceDetectors()
    {
        var enabled = new List<ResourceDetector>();
        var properties = typeof(DotNetDetectors).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var prop in properties)
        {
            var value = prop.GetValue(this);
            if (value != null)
            {
                if (Enum.TryParse<ResourceDetector>(prop.Name, out var resourceDetector))
                {
                    enabled.Add(resourceDetector);
                }
            }
        }

        return enabled;
    }
}

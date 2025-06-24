// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class DotNetDetectors
{
    /// <summary>
    /// Gets or sets the Azure App Service detector configuration.
    /// </summary>
    [YamlMember(Alias = "azureappservice")]
    public object? AzureAppService { get; set; }

    /// <summary>
    /// Gets or sets the container detector configuration.
    /// </summary>
    [YamlMember(Alias = "container")]
    public object? Container { get; set; }

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
}

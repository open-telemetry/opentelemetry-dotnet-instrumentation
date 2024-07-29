// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Global Settings
/// </summary>
internal abstract class Settings
{
    public static T FromDefaultSources<T>(bool failFast)
        where T : Settings, new()
    {
        var configuration = new Configuration(failFast, new EnvironmentConfigurationSource(failFast));
        var settings = new T();
        settings.Load(configuration);
        return settings;
    }

    public void Load(Configuration configuration)
    {
        OnLoad(configuration);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Settings"/> class
    /// using the specified <see cref="Configuration"/> to initialize values.
    /// </summary>
    /// <param name="configuration">The <see cref="Configuration"/> to use when retrieving configuration values.</param>
    protected abstract void OnLoad(Configuration configuration);
}

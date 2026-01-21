// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class GeneralSettings : Settings
{
    /// <summary>
    /// Gets a value indicating whether the <see cref="AppDomain.UnhandledException"/> event should trigger
    /// the flushing of telemetry data.
    /// Default is <c>false</c>.
    /// </summary>
    public bool FlushOnUnhandledException { get; private set; }

    /// <summary>
    /// Gets a value indicating whether OpenTelemetry .NET SDK should be set up.
    /// </summary>
    public bool SetupSdk { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the profiler is enabled.
    /// </summary>
    public bool ProfilerEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the .NET Framework assembly redirection is enabled.
    /// </summary>
    public bool NetFxRedirectEnabled { get; private set; }

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        FlushOnUnhandledException = configuration.GetBool(ConfigurationKeys.FlushOnUnhandledException) ?? false;
        SetupSdk = configuration.GetBool(ConfigurationKeys.SetupSdk) ?? true;
        NetFxRedirectEnabled = configuration.GetBool(ConfigurationKeys.NetFxRedirectEnabled) ?? true;

        ProfilerEnabled = configuration.GetString(ConfigurationKeys.ProfilingEnabled) == "1";
    }

    protected override void OnLoadFile(YamlConfiguration configuration)
    {
        SetupSdk = !configuration.Disabled;
        FlushOnUnhandledException = configuration.FlushOnUnhandledException;
        NetFxRedirectEnabled = configuration.NetFxRedirectEnabled;

        // Using the environment variable instead of YamlConfiguration because the default.NET environment variable
        // is used for enabling the profiler, and without this environment variable, the profiler will not work.
        ProfilerEnabled = Environment.GetEnvironmentVariable(ConfigurationKeys.ProfilingEnabled) == "1";
    }
}

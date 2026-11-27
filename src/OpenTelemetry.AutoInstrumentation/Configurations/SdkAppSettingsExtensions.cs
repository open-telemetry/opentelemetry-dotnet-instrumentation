// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#endif
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Makes the current AppDomain's appSettings visible to settings the OpenTelemetry SDK reads itself.
/// No-op on .NET and when file-based configuration is used.
/// </summary>
internal static class SdkAppSettingsExtensions
{
    public static TracerProviderBuilder UseAppSettingsForSdk(this TracerProviderBuilder builder)
    {
#if NETFRAMEWORK
        if (!Settings.IsYamlConfigEnabled)
        {
            builder.ConfigureServices(static services => services.AddSingleton<IConfiguration>(CreateConfiguration()));
        }
#endif
        return builder;
    }

    public static MeterProviderBuilder UseAppSettingsForSdk(this MeterProviderBuilder builder)
    {
#if NETFRAMEWORK
        if (!Settings.IsYamlConfigEnabled)
        {
            builder.ConfigureServices(static services => services.AddSingleton<IConfiguration>(CreateConfiguration()));
        }
#endif
        return builder;
    }

    public static LoggerProviderBuilder UseAppSettingsForSdk(this LoggerProviderBuilder builder)
    {
#if NETFRAMEWORK
        if (!Settings.IsYamlConfigEnabled)
        {
            builder.ConfigureServices(static services => services.AddSingleton<IConfiguration>(CreateConfiguration()));
        }
#endif
        return builder;
    }

#if NETFRAMEWORK
    private static IConfiguration CreateConfiguration() => SdkAppSettingsConfiguration.Create(System.Configuration.ConfigurationManager.AppSettings);
#endif
}

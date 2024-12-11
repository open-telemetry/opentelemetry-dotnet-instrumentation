// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class EnvironmentConfigurationLogHelper
{
    public static LoggerProviderBuilder UseEnvironmentVariables(
        this LoggerProviderBuilder builder,
        LazyInstrumentationLoader lazyInstrumentationLoader,
        LogSettings settings,
        PluginManager pluginManager)
    {
        SetExporter(builder, settings, pluginManager);
        return builder;
    }

    private static void SetExporter(LoggerProviderBuilder builder, LogSettings settings, PluginManager pluginManager)
    {
        foreach (var logExporter in settings.LogExporters)
        {
            builder = logExporter switch
            {
                LogExporter.Otlp => Wrappers.AddOtlpExporter(builder, settings, pluginManager),
                LogExporter.Console => Wrappers.AddConsoleExporter(builder, pluginManager),
                _ => throw new ArgumentOutOfRangeException($"Log exporter '{logExporter}' is incorrect")
            };
        }
    }

    private static class Wrappers
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static LoggerProviderBuilder AddConsoleExporter(LoggerProviderBuilder builder, PluginManager pluginManager)
        {
            return builder.AddConsoleExporter(pluginManager.ConfigureLogsOptions);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static LoggerProviderBuilder AddOtlpExporter(LoggerProviderBuilder builder, LogSettings settings, PluginManager pluginManager)
        {
            return builder.AddOtlpExporter(options =>
            {
                settings.OtlpSettings?.CopyTo(options);
                pluginManager?.ConfigureLogsOptions(options);
            });
        }
    }
}

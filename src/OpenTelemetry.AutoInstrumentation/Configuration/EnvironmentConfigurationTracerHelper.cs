// <copyright file="EnvironmentConfigurationTracerHelper.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

internal static class EnvironmentConfigurationTracerHelper
{
    public static TracerProviderBuilder UseEnvironmentVariables(
        this TracerProviderBuilder builder,
        LazyInstrumentationLoader lazyInstrumentationLoader,
        TracerSettings settings,
        PluginManager pluginManager)
    {
        builder.SetExporter(settings, pluginManager);

        foreach (var enabledInstrumentation in settings.EnabledInstrumentations)
        {
            _ = enabledInstrumentation switch
            {
                TracerInstrumentation.AspNet => Wrappers.AddAspNetInstrumentation(builder, pluginManager, lazyInstrumentationLoader),
                TracerInstrumentation.GrpcNetClient => Wrappers.AddGrpcClientInstrumentation(builder, pluginManager),
                TracerInstrumentation.HttpClient => Wrappers.AddHttpClientInstrumentation(builder, pluginManager),
                TracerInstrumentation.Npgsql => builder.AddSource("Npgsql"),
                TracerInstrumentation.SqlClient => Wrappers.AddSqlClientInstrumentation(builder, pluginManager),
                TracerInstrumentation.Wcf => Wrappers.AddWcfInstrumentation(builder, pluginManager, lazyInstrumentationLoader),
#if NET6_0_OR_GREATER
                TracerInstrumentation.MassTransit => builder.AddSource("MassTransit"),
                TracerInstrumentation.MongoDB => builder.AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources"),
                TracerInstrumentation.MySqlData => Wrappers.AddMySqlClientInstrumentation(builder, pluginManager, lazyInstrumentationLoader),
                TracerInstrumentation.StackExchangeRedis => builder.AddSource("OpenTelemetry.Instrumentation.StackExchangeRedis"),
#endif
                _ => null
            };
        }

        builder.AddSource(settings.ActivitySources.ToArray());
        foreach (var legacySource in settings.LegacySources)
        {
            builder.AddLegacySource(legacySource);
        }

        return builder;
    }

    private static TracerProviderBuilder SetExporter(this TracerProviderBuilder builder, TracerSettings settings, PluginManager pluginManager)
    {
        if (settings.ConsoleExporterEnabled)
        {
            Wrappers.AddConsoleExporter(builder, pluginManager);
        }

        return settings.TracesExporter switch
        {
            TracesExporter.Zipkin => Wrappers.AddZipkinExporter(builder, pluginManager),
            TracesExporter.Jaeger => Wrappers.AddJaegerExporter(builder, pluginManager),
            TracesExporter.Otlp => Wrappers.AddOtlpExporter(builder, settings, pluginManager),
            TracesExporter.None => builder,
            _ => throw new ArgumentOutOfRangeException($"Traces exporter '{settings.TracesExporter}' is incorrect")
        };
    }

    /// <summary>
    /// This class wraps external extension methods to ensure the dlls are not loaded, if not necessary.
    /// .NET Framework is aggressively inlining these wrappers. Inlining must be disabled to ensure the wrapping effect.
    /// </summary>
    private static class Wrappers
    {
        // Instrumentations

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddWcfInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader)
        {
            lazyInstrumentationLoader.Add(new WcfInitializer(pluginManager));

            return builder.AddSource("OpenTelemetry.Instrumentation.Wcf");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddHttpClientInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager)
        {
            return builder.AddHttpClientInstrumentation(pluginManager.ConfigureOptions);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddAspNetInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader)
        {
#if NET462
            new AspNetInitializer(lazyInstrumentationLoader, pluginManager);

            builder.AddSource(OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule.AspNetSourceName);
#elif NET6_0_OR_GREATER
            lazyInstrumentationLoader.Add(new AspNetCoreInitializer(pluginManager));

            builder.AddSource("OpenTelemetry.Instrumentation.AspNetCore");
            builder.AddLegacySource("Microsoft.AspNetCore.Hosting.HttpRequestIn");
#endif

            return builder;
        }

#if NET6_0_OR_GREATER
        public static TracerProviderBuilder AddMySqlClientInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader)
        {
            lazyInstrumentationLoader.Add(new MySqlDataInitializer(pluginManager));

            return builder.AddSource("OpenTelemetry.Instrumentation.MySqlData");
        }
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddSqlClientInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager)
        {
            return builder.AddSqlClientInstrumentation(pluginManager.ConfigureOptions);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddGrpcClientInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager)
        {
            return builder.AddGrpcClientInstrumentation(options =>
            {
                options.SuppressDownstreamInstrumentation = !Instrumentation.TracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.HttpClient);
                pluginManager.ConfigureOptions(options);
            });
        }

        // Exporters

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddConsoleExporter(TracerProviderBuilder builder, PluginManager pluginManager)
        {
            return builder.AddConsoleExporter(pluginManager.ConfigureOptions);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddZipkinExporter(TracerProviderBuilder builder, PluginManager pluginManager)
        {
            return builder.AddZipkinExporter(pluginManager.ConfigureOptions);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddJaegerExporter(TracerProviderBuilder builder, PluginManager pluginManager)
        {
            return builder.AddJaegerExporter(pluginManager.ConfigureOptions);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddOtlpExporter(TracerProviderBuilder builder, TracerSettings settings, PluginManager pluginManager)
        {
            return builder.AddOtlpExporter(options =>
            {
                if (settings.OtlpExportProtocol.HasValue)
                {
                    options.Protocol = settings.OtlpExportProtocol.Value;
                    pluginManager.ConfigureOptions(options);
                }
            });
        }
    }
}

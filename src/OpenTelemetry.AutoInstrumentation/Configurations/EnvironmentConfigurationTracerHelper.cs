// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class EnvironmentConfigurationTracerHelper
{
    public static TracerProviderBuilder UseEnvironmentVariables(
        this TracerProviderBuilder builder,
        LazyInstrumentationLoader lazyInstrumentationLoader,
        TracerSettings settings,
        PluginManager pluginManager)
    {
        // ensure WcfInitializer is added only once,
        // it is needed when either WcfClient or WcfService instrumentations are enabled
        // to initialize WcfInstrumentationOptions
        var wcfInstrumentationAdded = false;
        foreach (var enabledInstrumentation in settings.EnabledInstrumentations)
        {
            _ = enabledInstrumentation switch
            {
#if NETFRAMEWORK
                TracerInstrumentation.AspNet => Wrappers.AddAspNetInstrumentation(builder, pluginManager, lazyInstrumentationLoader),
                TracerInstrumentation.WcfService => AddWcfIfNeeded(builder, pluginManager, lazyInstrumentationLoader, ref wcfInstrumentationAdded),
#endif
                TracerInstrumentation.GrpcNetClient => Wrappers.AddGrpcClientInstrumentation(builder, pluginManager, lazyInstrumentationLoader),
                TracerInstrumentation.HttpClient => Wrappers.AddHttpClientInstrumentation(builder, pluginManager, lazyInstrumentationLoader),
                TracerInstrumentation.Npgsql => builder.AddSource("Npgsql"),
                TracerInstrumentation.SqlClient => Wrappers.AddSqlClientInstrumentation(builder, pluginManager, lazyInstrumentationLoader, settings),
                TracerInstrumentation.NServiceBus => builder.AddSource("NServiceBus.Core"),
                TracerInstrumentation.Elasticsearch => builder.AddSource("Elastic.Clients.Elasticsearch.ElasticsearchClient"),
                TracerInstrumentation.ElasticTransport => builder.AddSource("Elastic.Transport"),
                TracerInstrumentation.Quartz => Wrappers.AddQuartzInstrumentation(builder, pluginManager, lazyInstrumentationLoader),
                TracerInstrumentation.MongoDB => builder.AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources"),
                TracerInstrumentation.MySqlConnector => builder.AddSource("MySqlConnector"),
                TracerInstrumentation.Azure => Wrappers.AddAzureInstrumentation(builder),
                TracerInstrumentation.WcfClient => AddWcfIfNeeded(builder, pluginManager, lazyInstrumentationLoader, ref wcfInstrumentationAdded),
#if NET6_0_OR_GREATER
                TracerInstrumentation.AspNetCore => Wrappers.AddAspNetCoreInstrumentation(builder, pluginManager, lazyInstrumentationLoader),
                TracerInstrumentation.MassTransit => builder.AddSource("MassTransit"),
                TracerInstrumentation.MySqlData => builder.AddSource("connector-net"),
                TracerInstrumentation.StackExchangeRedis => builder.AddSource("OpenTelemetry.Instrumentation.StackExchangeRedis"),
                TracerInstrumentation.EntityFrameworkCore => Wrappers.AddEntityFrameworkCoreInstrumentation(builder, pluginManager, lazyInstrumentationLoader, settings),
                TracerInstrumentation.GraphQL => Wrappers.AddGraphQLInstrumentation(builder, pluginManager, lazyInstrumentationLoader, settings),
#endif
                _ => null
            };
        }

        if (settings.OpenTracingEnabled)
        {
            builder.AddOpenTracingShimSource();
        }

        builder
            .SetSampler(settings)
            // Exporters can cause dependency loads.
            // Should be called later if dependency listeners are already setup.
            .SetExporter(settings, pluginManager)
            .AddSource(settings.ActivitySources.ToArray());

        foreach (var legacySource in settings.AdditionalLegacySources)
        {
            builder.AddLegacySource(legacySource);
        }

        return builder;
    }

    private static TracerProviderBuilder AddWcfIfNeeded(
        TracerProviderBuilder tracerProviderBuilder,
        PluginManager pluginManager,
        LazyInstrumentationLoader lazyInstrumentationLoader,
        ref bool wcfInstrumentationAdded)
    {
        if (wcfInstrumentationAdded)
        {
            return tracerProviderBuilder;
        }

        Wrappers.AddWcfInstrumentation(tracerProviderBuilder, pluginManager, lazyInstrumentationLoader);
        wcfInstrumentationAdded = true;

        return tracerProviderBuilder;
    }

    private static TracerProviderBuilder SetSampler(this TracerProviderBuilder builder, TracerSettings settings)
    {
        if (settings.TracesSampler == null)
        {
            return builder;
        }

        var sampler = TracerSamplerHelper.GetSampler(settings.TracesSampler, settings.TracesSamplerArguments);

        if (sampler == null)
        {
            return builder;
        }

        return builder.SetSampler(sampler);
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
            DelayedInitialization.Traces.AddWcf(lazyInstrumentationLoader, pluginManager);

            return builder.AddSource("OpenTelemetry.Instrumentation.Wcf");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddHttpClientInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader)
        {
            DelayedInitialization.Traces.AddHttpClient(lazyInstrumentationLoader, pluginManager);

#if NETFRAMEWORK
            builder.AddSource("OpenTelemetry.Instrumentation.Http.HttpWebRequest");
#else
            builder.AddSource("OpenTelemetry.Instrumentation.Http.HttpClient");
            builder.AddSource("System.Net.Http"); // This works only System.Net.Http >= 7.0.0
            builder.AddLegacySource("System.Net.Http.HttpRequestOut");
#endif

            return builder;
        }

#if NETFRAMEWORK
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddAspNetInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader)
        {
            DelayedInitialization.Traces.AddAspNet(lazyInstrumentationLoader, pluginManager);
            return builder.AddSource(OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule.AspNetSourceName);
        }
#endif

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddAspNetCoreInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader)
        {
            DelayedInitialization.Traces.AddAspNetCore(lazyInstrumentationLoader, pluginManager);

            if (Environment.Version.Major == 6)
            {
                return builder
                    .AddSource("OpenTelemetry.Instrumentation.AspNetCore")
                    .AddLegacySource("Microsoft.AspNetCore.Hosting.HttpRequestIn");
            }

            return builder.AddSource("Microsoft.AspNetCore");
        }

        public static TracerProviderBuilder AddGraphQLInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader, TracerSettings tracerSettings)
        {
            DelayedInitialization.Traces.AddGraphQL(lazyInstrumentationLoader, pluginManager, tracerSettings);

            return builder.AddSource("GraphQL");
        }
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddAzureInstrumentation(TracerProviderBuilder builder)
        {
            AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
            return builder.AddSource("Azure.*");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddSqlClientInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader, TracerSettings tracerSettings)
        {
            DelayedInitialization.Traces.AddSqlClient(lazyInstrumentationLoader, pluginManager, tracerSettings);

            return builder.AddSource("OpenTelemetry.Instrumentation.SqlClient");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddGrpcClientInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader)
        {
            DelayedInitialization.Traces.AddGrpcClient(lazyInstrumentationLoader, pluginManager);

            builder.AddSource("OpenTelemetry.Instrumentation.GrpcNetClient");
            builder.AddLegacySource("Grpc.Net.Client.GrpcOut");

            return builder;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddQuartzInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader)
        {
            DelayedInitialization.Traces.AddQuartz(lazyInstrumentationLoader, pluginManager);

            return builder.AddSource("OpenTelemetry.Instrumentation.Quartz")
                .AddLegacySource("Quartz.Job.Execute")
                .AddLegacySource("Quartz.Job.Veto");
        }

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddEntityFrameworkCoreInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader, TracerSettings tracerSettings)
        {
            DelayedInitialization.Traces.AddEntityFrameworkCore(lazyInstrumentationLoader, pluginManager, tracerSettings);

            return builder.AddSource("OpenTelemetry.Instrumentation.EntityFrameworkCore");
        }
#endif

        // Exporters

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddConsoleExporter(TracerProviderBuilder builder, PluginManager pluginManager)
        {
            return builder.AddConsoleExporter(pluginManager.ConfigureTracesOptions);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddZipkinExporter(TracerProviderBuilder builder, PluginManager pluginManager)
        {
            return builder.AddZipkinExporter(pluginManager.ConfigureTracesOptions);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddOtlpExporter(TracerProviderBuilder builder, TracerSettings settings, PluginManager pluginManager)
        {
            return builder.AddOtlpExporter(options =>
            {
                if (settings.OtlpExportProtocol.HasValue)
                {
                    options.Protocol = settings.OtlpExportProtocol.Value;
                }

                pluginManager.ConfigureTracesOptions(options);
            });
        }
    }
}

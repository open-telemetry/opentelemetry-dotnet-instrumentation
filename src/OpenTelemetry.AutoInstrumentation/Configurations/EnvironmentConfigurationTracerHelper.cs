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
        // ensure WCF instrumentation activity source is added only once,
        // it is needed when either WcfClient or WcfService instrumentations are enabled
        var wcfInstrumentationAdded = false;
        foreach (var enabledInstrumentation in settings.EnabledInstrumentations)
        {
            _ = enabledInstrumentation switch
            {
#if NETFRAMEWORK
                TracerInstrumentation.AspNet => Wrappers.AddAspNetInstrumentation(builder, pluginManager, lazyInstrumentationLoader, settings),
                TracerInstrumentation.WcfService => AddWcfIfNeeded(builder, ref wcfInstrumentationAdded),
#endif
                TracerInstrumentation.GrpcNetClient => Wrappers.AddGrpcClientInstrumentation(builder, pluginManager, lazyInstrumentationLoader, settings),
                TracerInstrumentation.HttpClient => Wrappers.AddHttpClientInstrumentation(builder, pluginManager, lazyInstrumentationLoader, settings),
                TracerInstrumentation.Npgsql => builder.AddSource("Npgsql"),
                TracerInstrumentation.SqlClient => Wrappers.AddSqlClientInstrumentation(builder, pluginManager, lazyInstrumentationLoader, settings),
                TracerInstrumentation.NServiceBus => builder.AddSource("NServiceBus.Core"),
                TracerInstrumentation.Elasticsearch => builder.AddSource("Elastic.Clients.Elasticsearch.ElasticsearchClient"),
                TracerInstrumentation.ElasticTransport => builder.AddSource("Elastic.Transport"),
                TracerInstrumentation.Quartz => Wrappers.AddQuartzInstrumentation(builder, pluginManager, lazyInstrumentationLoader),
                TracerInstrumentation.MySqlConnector => builder.AddSource("MySqlConnector"),
                TracerInstrumentation.Azure => Wrappers.AddAzureInstrumentation(builder),
                TracerInstrumentation.WcfClient => AddWcfIfNeeded(builder, ref wcfInstrumentationAdded),
                TracerInstrumentation.OracleMda => Wrappers.AddOracleMdaInstrumentation(builder, lazyInstrumentationLoader, settings),
                TracerInstrumentation.RabbitMq => builder.AddSource("RabbitMQ.Client.Publisher").AddSource("RabbitMQ.Client.Subscriber"),
#if NET
                TracerInstrumentation.AspNetCore => Wrappers.AddAspNetCoreInstrumentation(builder, pluginManager, lazyInstrumentationLoader, settings),
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
        ref bool wcfInstrumentationAdded)
    {
        if (wcfInstrumentationAdded)
        {
            return tracerProviderBuilder;
        }

        tracerProviderBuilder.AddSource("OpenTelemetry.Instrumentation.Wcf");
        wcfInstrumentationAdded = true;

        return tracerProviderBuilder;
    }

    private static TracerProviderBuilder SetExporter(this TracerProviderBuilder builder, TracerSettings settings, PluginManager pluginManager)
    {
        foreach (var traceExporter in settings.TracesExporters)
        {
            builder = traceExporter switch
            {
                TracesExporter.Zipkin => Wrappers.AddZipkinExporter(builder, pluginManager),
                TracesExporter.Otlp => Wrappers.AddOtlpExporter(builder, settings, pluginManager),
                TracesExporter.Console => Wrappers.AddConsoleExporter(builder, pluginManager),
                _ => throw new ArgumentOutOfRangeException($"Traces exporter '{traceExporter}' is incorrect")
            };
        }

        return builder;
    }

    /// <summary>
    /// This class wraps external extension methods to ensure the dlls are not loaded, if not necessary.
    /// .NET Framework is aggressively inlining these wrappers. Inlining must be disabled to ensure the wrapping effect.
    /// </summary>
    private static class Wrappers
    {
        // Instrumentations

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddHttpClientInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader, TracerSettings tracerSettings)
        {
            DelayedInitialization.Traces.AddHttpClient(lazyInstrumentationLoader, pluginManager, tracerSettings);

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
        public static TracerProviderBuilder AddAspNetInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader, TracerSettings tracerSettings)
        {
            DelayedInitialization.Traces.AddAspNet(lazyInstrumentationLoader, pluginManager, tracerSettings);
            return builder.AddSource(OpenTelemetry.Instrumentation.AspNet.TelemetryHttpModule.AspNetSourceName);
        }
#endif

#if NET
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddAspNetCoreInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader, TracerSettings tracerSettings)
        {
            DelayedInitialization.Traces.AddAspNetCore(lazyInstrumentationLoader, pluginManager, tracerSettings);
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
        public static TracerProviderBuilder AddOracleMdaInstrumentation(TracerProviderBuilder builder, LazyInstrumentationLoader lazyInstrumentationLoader, TracerSettings tracerSettings)
        {
            DelayedInitialization.Traces.AddOracleMda(lazyInstrumentationLoader, tracerSettings);

#if NETFRAMEWORK
            return builder.AddSource("Oracle.ManagedDataAccess");
#else
            return builder.AddSource("Oracle.ManagedDataAccess.Core");
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddGrpcClientInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader, TracerSettings tracerSettings)
        {
            DelayedInitialization.Traces.AddGrpcClient(lazyInstrumentationLoader, pluginManager, tracerSettings);

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

#if NET
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
                // Copy Auto settings to SDK settings
                settings.OtlpSettings?.CopyTo(options);

                pluginManager.ConfigureTracesOptions(options);
            });
        }
    }
}

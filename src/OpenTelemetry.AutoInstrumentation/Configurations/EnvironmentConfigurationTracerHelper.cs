// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Configurations.Otlp;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class EnvironmentConfigurationTracerHelper
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

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
            Logger.Warning("OpenTracing is deprecated and it is enabled by the configuration. It will be removed in future versions. Consider migrating to OpenTelemetry API.");
            builder.AddOpenTracingShimSource();
        }

        builder = builder
            // Exporters can cause dependency loads.
            // Should be called later if dependency listeners are already setup.
            .SetExporter(settings, pluginManager);

        if (settings.Sampler != null)
        {
            builder = builder.SetSampler(settings.Sampler);
        }

        builder = builder.AddSource([.. settings.ActivitySources]);

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
        // If no exporters are specified, it means to use processors (file-based configuration).
        if (settings.TracesExporters.Count == 0)
        {
            if (settings.Processors != null)
            {
                foreach (var processor in settings.Processors)
                {
                    if (processor.Batch != null && processor.Simple != null)
                    {
                        Logger.Debug("Both batch and simple tracer processors are configured. It is not supported. Skipping.");
                        continue;
                    }

                    if (processor.Batch == null && processor.Simple == null)
                    {
                        Logger.Debug("No valid tracer processor configured, skipping.");
                        continue;
                    }

                    if (processor.Batch != null)
                    {
                        var exporter = processor.Batch.Exporter;
                        if (exporter == null)
                        {
                            Logger.Debug("No exporter section for batch tracer processor. Skipping.");
                            continue;
                        }

                        var exportersCount = 0;

                        if (exporter.OtlpHttp != null)
                        {
                            exportersCount++;
                        }

                        if (exporter.OtlpGrpc != null)
                        {
                            exportersCount++;
                        }

                        if (exporter.Zipkin != null)
                        {
                            exportersCount++;
                        }

                        switch (exportersCount)
                        {
                            case 0:
                                Logger.Debug("No valid exporter configured for batch tracer processor. Skipping.");
                                continue;
                            case > 1:
                                Logger.Debug("Multiple exporters are configured for batch tracer processor. Only one exporter is supported. Skipping.");
                                continue;
                        }

                        if (exporter.OtlpHttp != null)
                        {
                            builder = Wrappers.AddOtlpHttpExporter(builder, pluginManager, processor.Batch, exporter.OtlpHttp);
                        }
                        else if (exporter.OtlpGrpc != null)
                        {
                            builder = Wrappers.AddOtlpGrpcExporter(builder, pluginManager, processor.Batch, exporter.OtlpGrpc);
                        }
                        else if (exporter.Zipkin != null)
                        {
                            builder = Wrappers.AddZipkinExporter(builder, pluginManager, processor.Batch, exporter.Zipkin);
                        }
                    }
                    else if (processor.Simple != null)
                    {
                        var exporter = processor.Simple.Exporter;
                        if (exporter == null)
                        {
                            Logger.Debug("No exporter section for simple tracer processor. Skipping.");
                            continue;
                        }

                        var exportersCount = 0;

                        if (exporter.OtlpHttp != null)
                        {
                            exportersCount++;
                        }

                        if (exporter.OtlpGrpc != null)
                        {
                            exportersCount++;
                        }

                        if (exporter.Zipkin != null)
                        {
                            exportersCount++;
                        }

                        if (exporter.Console != null)
                        {
                            exportersCount++;
                        }

                        switch (exportersCount)
                        {
                            case 0:
                                Logger.Debug("No valid exporter configured for simple tracer processor. Skipping.");
                                continue;
                            case > 1:
                                Logger.Debug("Multiple exporters are configured for simple tracer processor. Only one exporter is supported. Skipping.");
                                continue;
                        }

                        if (exporter.OtlpHttp != null)
                        {
                            builder = Wrappers.AddOtlpHttpExporter(builder, pluginManager, exporter.OtlpHttp);
                        }
                        else if (exporter.OtlpGrpc != null)
                        {
                            builder = Wrappers.AddOtlpGrpcExporter(builder, pluginManager, exporter.OtlpGrpc);
                        }
                        else if (exporter.Zipkin != null)
                        {
                            builder = Wrappers.AddZipkinExporter(builder, pluginManager, exporter.Zipkin);
                        }
                        else if (exporter.Console != null)
                        {
                            builder = Wrappers.AddConsoleExporter(builder, pluginManager);
                        }
                    }
                }
            }
        }
        else
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
            return builder.AddSource("OpenTelemetry.Instrumentation.AspNet");
        }
#endif

#if NET
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddAspNetCoreInstrumentation(TracerProviderBuilder builder, PluginManager pluginManager, LazyInstrumentationLoader lazyInstrumentationLoader, TracerSettings tracerSettings)
        {
            DelayedInitialization.Traces.AddAspNetCore(lazyInstrumentationLoader, pluginManager, tracerSettings);
            return builder.AddSource(
                "Microsoft.AspNetCore",
                // Blazor activities first added in .NET 10.0
                "Microsoft.AspNetCore.Components",
                "Microsoft.AspNetCore.Components.Server.Circuits");
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
            Logger.Warning("Zipkin exporter is deprecated and it is enabled by the configuration. It will be removed in future versions. Consider migrating to OTLP exporter.");
            return builder.AddZipkinExporter(pluginManager.ConfigureTracesOptions);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddOtlpExporter(TracerProviderBuilder builder, TracerSettings settings, PluginManager pluginManager)
        {
            return builder.AddOtlpExporter(options =>
            {
                settings.OtlpSettings?.CopyTo(options);

                pluginManager.ConfigureTracesOptions(options);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddOtlpHttpExporter(TracerProviderBuilder builder, PluginManager pluginManager, BatchProcessorConfig batch, OtlpHttpExporterConfig otlpHttp)
        {
            var otlpSettings = new OtlpSettings(OtlpSignalType.Traces, otlpHttp);
            return builder.AddOtlpExporter(options =>
            {
                // Copy Auto settings to SDK settings
                batch?.CopyTo(options.BatchExportProcessorOptions);
                otlpSettings?.CopyTo(options);

                pluginManager.ConfigureTracesOptions(options);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddOtlpGrpcExporter(TracerProviderBuilder builder, PluginManager pluginManager, BatchProcessorConfig batch, OtlpGrpcExporterConfig otlpGrpc)
        {
            var otlpSettings = new OtlpSettings(otlpGrpc);
            return builder.AddOtlpExporter(options =>
            {
                // Copy Auto settings to SDK settings
                batch?.CopyTo(options.BatchExportProcessorOptions);
                otlpSettings?.CopyTo(options);

                pluginManager.ConfigureTracesOptions(options);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddZipkinExporter(TracerProviderBuilder builder, PluginManager pluginManager, BatchProcessorConfig batch, ZipkinExporterConfig zipkin)
        {
            Logger.Warning("Zipkin exporter is deprecated and it is enabled by the configuration. It will be removed in future versions. Consider migrating to OTLP exporter.");
            return builder.AddZipkinExporter(options =>
            {
                // Copy Auto settings to SDK settings
                batch?.CopyTo(options.BatchExportProcessorOptions);
                zipkin?.CopyTo(options);

                pluginManager.ConfigureTracesOptions(options);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddOtlpHttpExporter(TracerProviderBuilder builder, PluginManager pluginManager, OtlpHttpExporterConfig otlpHttp)
        {
            var otlpSettings = new OtlpSettings(OtlpSignalType.Traces, otlpHttp);
            return builder.AddOtlpExporter(options =>
            {
                // Copy Auto settings to SDK settings
                options.ExportProcessorType = ExportProcessorType.Simple;
                otlpSettings?.CopyTo(options);

                pluginManager.ConfigureTracesOptions(options);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddOtlpGrpcExporter(TracerProviderBuilder builder, PluginManager pluginManager, OtlpGrpcExporterConfig otlpGrpc)
        {
            var otlpSettings = new OtlpSettings(otlpGrpc);
            return builder.AddOtlpExporter(options =>
            {
                // Copy Auto settings to SDK settings
                options.ExportProcessorType = ExportProcessorType.Simple;
                otlpSettings?.CopyTo(options);

                pluginManager.ConfigureTracesOptions(options);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddZipkinExporter(TracerProviderBuilder builder, PluginManager pluginManager, ZipkinExporterConfig zipkin)
        {
            Logger.Warning("Zipkin exporter is deprecated and it is enabled by the configuration. It will be removed in future versions. Consider migrating to OTLP exporter.");
            return builder.AddZipkinExporter(options =>
            {
                // Copy Auto settings to SDK settings
                options.ExportProcessorType = ExportProcessorType.Simple;
                zipkin?.CopyTo(options);

                pluginManager.ConfigureTracesOptions(options);
            });
        }
    }
}

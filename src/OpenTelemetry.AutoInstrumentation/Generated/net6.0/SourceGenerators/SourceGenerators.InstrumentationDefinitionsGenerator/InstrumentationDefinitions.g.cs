﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the InstrumentationDefinitionsGenerator tool. To safely
//     modify this file, edit InstrumentMethodAttribute on the classes and
//     compile project.

//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated. 
// </auto-generated>
//------------------------------------------------------------------------------

using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation;

internal static partial class InstrumentationDefinitions
{
    private static readonly string AssemblyFullName = typeof(InstrumentationDefinitions).Assembly.FullName!;

    private static NativeCallTargetDefinition[] GetDefinitionsArray()
    {
        var nativeCallTargetDefinitions = new List<NativeCallTargetDefinition>(24);
        // Traces
        var tracerSettings = Instrumentation.TracerSettings.Value;
        if (tracerSettings.TracesEnabled)
        {
            // Kafka
            if (tracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.Kafka))
            {
                nativeCallTargetDefinitions.Add(new("Confluent.Kafka", "Confluent.Kafka.Consumer`2", "Close", new[] {"System.Void"}, 1, 4, 0, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations.ConsumerCloseIntegration"));
                nativeCallTargetDefinitions.Add(new("Confluent.Kafka", "Confluent.Kafka.Consumer`2", ".ctor", new[] {"System.Void", "Confluent.Kafka.ConsumerBuilder`2[!0,!1]"}, 1, 4, 0, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations.ConsumerConstructorIntegration"));
                nativeCallTargetDefinitions.Add(new("Confluent.Kafka", "Confluent.Kafka.Consumer`2", "Consume", new[] {"Confluent.Kafka.ConsumeResult`2[!0,!1]", "System.Int32"}, 1, 4, 0, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations.ConsumerConsumeSyncIntegration"));
                nativeCallTargetDefinitions.Add(new("Confluent.Kafka", "Confluent.Kafka.Consumer`2", "Dispose", new[] {"System.Void"}, 1, 4, 0, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations.ConsumerDisposeIntegration"));
                nativeCallTargetDefinitions.Add(new("Confluent.Kafka", "Confluent.Kafka.Consumer`2", "Unsubscribe", new[] {"System.Void", "Confluent.Kafka.ConsumerBuilder`2[!0,!1]"}, 1, 4, 0, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations.ConsumerUnsubscribeIntegration"));
                nativeCallTargetDefinitions.Add(new("Confluent.Kafka", "Confluent.Kafka.Producer`2+TypedDeliveryHandlerShim_Action", ".ctor", new[] {"System.Void", "System.String", "!0", "!1", "System.Action`1[Confluent.Kafka.DeliveryReport`2[!0,!1]]"}, 1, 4, 0, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations.ProducerDeliveryHandlerActionIntegration"));
                nativeCallTargetDefinitions.Add(new("Confluent.Kafka", "Confluent.Kafka.Producer`2", "Produce", new[] {"System.Void", "Confluent.Kafka.TopicPartition", "Confluent.Kafka.Message`2[!0,!1]", "System.Action`1[Confluent.Kafka.DeliveryReport`2[!0,!1]]"}, 1, 4, 0, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations.ProducerProduceSyncIntegration"));
            }

            // MongoDB
            if (tracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.MongoDB))
            {
                nativeCallTargetDefinitions.Add(new("MongoDB.Driver", "MongoDB.Driver.MongoClient", ".ctor", new[] {"System.Void", "MongoDB.Driver.MongoClientSettings"}, 2, 13, 3, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.MongoClientIntegration"));
            }

            // NServiceBus
            if (tracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.NServiceBus))
            {
                nativeCallTargetDefinitions.Add(new("NServiceBus.Core", "NServiceBus.EndpointConfiguration", ".ctor", new[] {"System.Void", "System.String"}, 8, 0, 0, 8, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.NServiceBus.EndpointConfigurationIntegration"));
            }

            // StackExchangeRedis
            if (tracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.StackExchangeRedis))
            {
                nativeCallTargetDefinitions.Add(new("StackExchange.Redis", "StackExchange.Redis.ConnectionMultiplexer", "ConnectImpl", new[] {"StackExchange.Redis.ConnectionMultiplexer", "System.Object", "System.IO.TextWriter"}, 2, 0, 0, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis.StackExchangeRedisIntegration"));
                nativeCallTargetDefinitions.Add(new("StackExchange.Redis", "StackExchange.Redis.ConnectionMultiplexer", "ConnectImpl", new[] {"StackExchange.Redis.ConnectionMultiplexer", "StackExchange.Redis.ConfigurationOptions", "System.IO.TextWriter"}, 2, 0, 0, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis.StackExchangeRedisIntegration"));
                nativeCallTargetDefinitions.Add(new("StackExchange.Redis", "StackExchange.Redis.ConnectionMultiplexer", "ConnectImpl", new[] {"StackExchange.Redis.ConnectionMultiplexer", "StackExchange.Redis.ConfigurationOptions", "System.IO.TextWriter", "System.Nullable`1[StackExchange.Redis.ServerType]"}, 2, 0, 0, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis.StackExchangeRedisIntegration"));
                nativeCallTargetDefinitions.Add(new("StackExchange.Redis", "StackExchange.Redis.ConnectionMultiplexer", "ConnectImpl", new[] {"StackExchange.Redis.ConnectionMultiplexer", "StackExchange.Redis.ConfigurationOptions", "System.IO.TextWriter", "System.Nullable`1[StackExchange.Redis.ServerType]", "StackExchange.Redis.EndPointCollection"}, 2, 0, 0, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis.StackExchangeRedisIntegration"));
                nativeCallTargetDefinitions.Add(new("StackExchange.Redis", "StackExchange.Redis.ConnectionMultiplexer", "ConnectImplAsync", new[] {"System.Threading.Tasks.Task`1[StackExchange.Redis.ConnectionMultiplexer]", "System.Object", "System.IO.TextWriter"}, 2, 0, 0, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis.StackExchangeRedisIntegrationAsync"));
                nativeCallTargetDefinitions.Add(new("StackExchange.Redis", "StackExchange.Redis.ConnectionMultiplexer", "ConnectImplAsync", new[] {"System.Threading.Tasks.Task`1[StackExchange.Redis.ConnectionMultiplexer]", "StackExchange.Redis.ConfigurationOptions", "System.IO.TextWriter"}, 2, 0, 0, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis.StackExchangeRedisIntegrationAsync"));
                nativeCallTargetDefinitions.Add(new("StackExchange.Redis", "StackExchange.Redis.ConnectionMultiplexer", "ConnectImplAsync", new[] {"System.Threading.Tasks.Task`1[StackExchange.Redis.ConnectionMultiplexer]", "StackExchange.Redis.ConfigurationOptions", "System.IO.TextWriter", "System.Nullable`1[StackExchange.Redis.ServerType]"}, 2, 0, 0, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis.StackExchangeRedisIntegrationAsync"));
            }

            // WcfClient
            if (tracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.WcfClient))
            {
                nativeCallTargetDefinitions.Add(new("System.Private.ServiceModel", "System.ServiceModel.ChannelFactory", "InitializeEndpoint", new[] {"System.Void", "System.String", "System.ServiceModel.EndpointAddress"}, 4, 0, 0, 4, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client.WcfClientIntegration"));
                nativeCallTargetDefinitions.Add(new("System.Private.ServiceModel", "System.ServiceModel.ChannelFactory", "InitializeEndpoint", new[] {"System.Void", "System.ServiceModel.Description.ServiceEndpoint"}, 4, 0, 0, 4, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client.WcfClientIntegration"));
                nativeCallTargetDefinitions.Add(new("System.Private.ServiceModel", "System.ServiceModel.ChannelFactory", "InitializeEndpoint", new[] {"System.Void", "System.ServiceModel.Channels.Binding", "System.ServiceModel.EndpointAddress"}, 4, 0, 0, 4, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client.WcfClientIntegration"));
                nativeCallTargetDefinitions.Add(new("System.ServiceModel.Primitives", "System.ServiceModel.ChannelFactory", "InitializeEndpoint", new[] {"System.Void", "System.String", "System.ServiceModel.EndpointAddress"}, 6, 0, 0, 6, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client.WcfClientIntegration"));
                nativeCallTargetDefinitions.Add(new("System.ServiceModel.Primitives", "System.ServiceModel.ChannelFactory", "InitializeEndpoint", new[] {"System.Void", "System.ServiceModel.Description.ServiceEndpoint"}, 6, 0, 0, 6, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client.WcfClientIntegration"));
                nativeCallTargetDefinitions.Add(new("System.ServiceModel.Primitives", "System.ServiceModel.ChannelFactory", "InitializeEndpoint", new[] {"System.Void", "System.ServiceModel.Channels.Binding", "System.ServiceModel.EndpointAddress"}, 6, 0, 0, 6, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client.WcfClientIntegration"));
            }
        }

        // Logs
        var logSettings = Instrumentation.LogSettings.Value;
        if (logSettings.LogsEnabled)
        {
            // ILogger
            if (logSettings.EnabledInstrumentations.Contains(LogInstrumentation.ILogger))
            {
                nativeCallTargetDefinitions.Add(new("Microsoft.Extensions.Logging", "Microsoft.Extensions.Logging.LoggingBuilder", ".ctor", new[] {"System.Void", "Microsoft.Extensions.DependencyInjection.IServiceCollection"}, 3, 1, 0, 8, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Logger.LoggingBuilderIntegration"));
            }
        }

        // Metrics
        var metricSettings = Instrumentation.MetricSettings.Value;
        if (metricSettings.MetricsEnabled)
        {
            // NServiceBus
            if (metricSettings.EnabledInstrumentations.Contains(MetricInstrumentation.NServiceBus))
            {
                nativeCallTargetDefinitions.Add(new("NServiceBus.Core", "NServiceBus.EndpointConfiguration", ".ctor", new[] {"System.Void", "System.String"}, 8, 0, 0, 8, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.NServiceBus.EndpointConfigurationIntegration"));
            }
        }

        return nativeCallTargetDefinitions.ToArray();
    }
}

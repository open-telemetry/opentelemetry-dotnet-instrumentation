// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka;

internal static class KafkaClusterIdCache
{
    // Sentinel: empty string means a fetch is in-flight; non-empty means resolved UUID.
    private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.Ordinal);

    /// <summary>
    /// Schedules a background fetch of the cluster ID for the given bootstrap servers string.
    /// No-ops if a fetch is already in-flight or the result is already cached.
    /// </summary>
    internal static void ScheduleFetch(string bootstrapServers, IReadOnlyList<KeyValuePair<string, string>>? config = null)
    {
        if (string.IsNullOrEmpty(bootstrapServers))
        {
            return;
        }

        // TryAdd succeeds only the first time – prevents duplicate fetches.
        if (!Cache.TryAdd(bootstrapServers, string.Empty))
        {
            return;
        }

        _ = Task.Run(() => FetchAndStore(bootstrapServers, config));
    }

    /// <summary>
    /// Returns the resolved cluster ID for <paramref name="bootstrapServers"/>,
    /// or <c>null</c> if the result is not yet available.
    /// </summary>
    internal static string? GetClusterId(string? bootstrapServers)
    {
        if (string.IsNullOrEmpty(bootstrapServers))
        {
            return null;
        }

        return Cache.TryGetValue(bootstrapServers, out var val) && !string.IsNullOrEmpty(val) ? val : null;
    }

    private static void FetchAndStore(string bootstrapServers, IReadOnlyList<KeyValuePair<string, string>>? config)
    {
        try
        {
            var clusterId = FetchClusterIdViaReflection(bootstrapServers, config);
            if (!string.IsNullOrEmpty(clusterId))
            {
                Cache[bootstrapServers] = clusterId!;
            }
            else
            {
                Cache.TryRemove(bootstrapServers, out _);
            }
        }
        catch
        {
            // If anything goes wrong, remove the sentinel so a future call can retry.
            Cache.TryRemove(bootstrapServers, out _);
        }
    }

    private static string? FetchClusterIdViaReflection(string bootstrapServers, IReadOnlyList<KeyValuePair<string, string>>? extraConfig)
    {
        // Confluent.Kafka is the instrumented assembly – it is already loaded in the process.
        var kafkaAssembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, IntegrationConstants.ConfluentKafkaAssemblyName, StringComparison.Ordinal));

        if (kafkaAssembly is null)
        {
            return null;
        }

        // Build AdminClientConfig { BootstrapServers = bootstrapServers, ...extraConfig }
        var adminClientConfigType = kafkaAssembly.GetType("Confluent.Kafka.AdminClientConfig");
        if (adminClientConfigType is null)
        {
            return null;
        }

        var configInstance = Activator.CreateInstance(adminClientConfigType);
        if (configInstance is null)
        {
            return null;
        }

        var bootstrapServersProp = adminClientConfigType.GetProperty("BootstrapServers", BindingFlags.Public | BindingFlags.Instance);
        bootstrapServersProp?.SetValue(configInstance, bootstrapServers);

        // Apply SSL/SASL and other config entries from the original producer/consumer config.
        if (extraConfig is not null)
        {
            // ClientConfig (parent of AdminClientConfig) exposes Set(string name, string value)
            // to set any rdkafka config property by name.
            var setMethod = adminClientConfigType.GetMethod(
                "Set",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [typeof(string), typeof(string)],
                null);
            if (setMethod is not null)
            {
                foreach (var kvp in extraConfig)
                {
                    if (!string.Equals(kvp.Key, "bootstrap.servers", StringComparison.OrdinalIgnoreCase))
                    {
                        setMethod.Invoke(configInstance, [kvp.Key, kvp.Value]);
                    }
                }
            }
        }

        // new AdminClientBuilder(config).Build()
        var adminClientBuilderType = kafkaAssembly.GetType("Confluent.Kafka.AdminClientBuilder");
        if (adminClientBuilderType is null)
        {
            return null;
        }

        var builderCtor = adminClientBuilderType.GetConstructor([typeof(IEnumerable<KeyValuePair<string, string>>)]);
        if (builderCtor is null)
        {
            return null;
        }

        var builderInstance = builderCtor.Invoke([configInstance]);
        var buildMethod = adminClientBuilderType.GetMethod("Build", BindingFlags.Public | BindingFlags.Instance, null, [], null);
        if (buildMethod is null)
        {
            return null;
        }

        var adminClientInstance = buildMethod.Invoke(builderInstance, null);
        if (adminClientInstance is null)
        {
            return null;
        }

        try
        {
            var adminClientType = adminClientInstance.GetType();

            // DescribeClusterAsync(DescribeClusterOptions options) → Task<DescribeClusterResult>
            // DescribeClusterResult.ClusterId is the correct API in Confluent.Kafka 2.x.
            // Metadata.ClusterId does NOT exist in 2.x (Metadata only has Brokers/Topics/OriginatingBroker*).
            var describeClusterOptionsType = kafkaAssembly.GetType("Confluent.Kafka.Admin.DescribeClusterOptions");
            if (describeClusterOptionsType is not null)
            {
                var describeClusterMethod = adminClientType.GetMethod(
                    "DescribeClusterAsync",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    [describeClusterOptionsType],
                    null);
                if (describeClusterMethod is not null)
                {
                    var taskObj = describeClusterMethod.Invoke(adminClientInstance, [null]);
                    if (taskObj is Task task)
                    {
                        if (!task.Wait(TimeSpan.FromSeconds(10))) return null;
                        var resultProp = taskObj.GetType().GetProperty("Result");
                        var describeResult = resultProp?.GetValue(taskObj);
                        var clusterIdProp = describeResult?.GetType()
                            .GetProperty("ClusterId", BindingFlags.Public | BindingFlags.Instance);
                        var clusterId = clusterIdProp?.GetValue(describeResult) as string;
                        if (!string.IsNullOrEmpty(clusterId))
                        {
                            return clusterId;
                        }
                    }
                }
            }

            // Fallback: GetMetadata for versions where Metadata.ClusterId may exist.
            var getMetadataMethod = adminClientType.GetMethod(
                "GetMetadata",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [typeof(TimeSpan)],
                null);
            if (getMetadataMethod is not null)
            {
                var metadata = getMetadataMethod.Invoke(adminClientInstance, [TimeSpan.FromSeconds(5)]);
                if (metadata is not null)
                {
                    var clusterIdProp = metadata.GetType()
                        .GetProperty("ClusterId", BindingFlags.Public | BindingFlags.Instance);
                    return clusterIdProp?.GetValue(metadata) as string;
                }
            }

            return null;
        }
        finally
        {
            // Dispose the AdminClient to release resources.
            if (adminClientInstance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}

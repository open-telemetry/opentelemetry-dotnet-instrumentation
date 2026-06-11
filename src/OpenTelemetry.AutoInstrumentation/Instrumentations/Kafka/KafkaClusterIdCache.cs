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
    internal static void ScheduleFetch(string bootstrapServers)
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

        _ = Task.Run(() => FetchAndStore(bootstrapServers));
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

    private static void FetchAndStore(string bootstrapServers)
    {
        try
        {
            var clusterId = FetchClusterIdViaReflection(bootstrapServers);
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

    private static string? FetchClusterIdViaReflection(string bootstrapServers)
    {
        // Confluent.Kafka is the instrumented assembly – it is already loaded in the process.
        var kafkaAssembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, IntegrationConstants.ConfluentKafkaAssemblyName, StringComparison.Ordinal));

        if (kafkaAssembly is null)
        {
            return null;
        }

        // Build AdminClientConfig { BootstrapServers = bootstrapServers }
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

        // new AdminClientBuilder(config).Build()
        var adminClientBuilderType = kafkaAssembly.GetType("Confluent.Kafka.AdminClientBuilder");
        if (adminClientBuilderType is null)
        {
            return null;
        }

        var builderCtor = adminClientBuilderType.GetConstructor([adminClientConfigType]);
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
            // adminClient.GetMetadata(TimeSpan.FromSeconds(5))
            var adminClientType = adminClientInstance.GetType();
            var getMetadataMethod = adminClientType.GetMethod("GetMetadata", BindingFlags.Public | BindingFlags.Instance, null, [typeof(TimeSpan)], null);
            if (getMetadataMethod is null)
            {
                return null;
            }

            var metadata = getMetadataMethod.Invoke(adminClientInstance, [TimeSpan.FromSeconds(5)]);
            if (metadata is null)
            {
                return null;
            }

            var clusterIdProp = metadata.GetType().GetProperty("ClusterId", BindingFlags.Public | BindingFlags.Instance);
            return clusterIdProp?.GetValue(metadata) as string;
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

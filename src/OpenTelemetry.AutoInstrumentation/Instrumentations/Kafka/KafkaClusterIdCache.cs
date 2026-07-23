// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka;

internal static class KafkaClusterIdCache
{
    private const long TtlMs = 60L * 60 * 1000;

    // Minimum spacing between fetch attempts for a given bootstrapServers while it is
    // unresolved or being refreshed. Prevents probing an unreachable broker on every span.
    private const long RetryFloorMs = 30L * 1000;

    // Only holds successfully resolved entries; no sentinel values.
    private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);

    // Key presence indicates a background fetch is currently running.
    private static readonly ConcurrentDictionary<string, byte> _inFlight = new(StringComparer.Ordinal);

    // Last fetch attempt time (success or failure) per bootstrapServers; rate-limits retries.
    private static readonly ConcurrentDictionary<string, long> _lastAttemptAt = new(StringComparer.Ordinal);

    // Auth config from the first constructor call per bootstrapServers; re-used for re-fetches.
    private static readonly ConcurrentDictionary<string, IReadOnlyList<KeyValuePair<string, string>>?> _configCache = new(StringComparer.Ordinal);

#if NET5_0_OR_GREATER
    private static long TickCountMs => Environment.TickCount64;
#else
    // net462: TickCount64 is .NET 5+; fall back to TickCount (int, wraps ~25 days).
    private static long TickCountMs => (long)Environment.TickCount;
#endif

    /// <summary>
    /// Schedules a background fetch of the cluster ID for the given bootstrap servers string.
    /// No-ops if a fetch is already in-flight, the cached value is still within its TTL, or a
    /// previous attempt was made within the retry floor. Triggers a (re-)fetch otherwise — both
    /// when no value has been resolved yet and when the cached value has expired.
    /// When <paramref name="clientInstance"/> is provided (fully-constructed producer/consumer),
    /// the fetch uses DependentAdminClientBuilder so no new rdkafka handles are allocated.
    /// </summary>
    internal static void ScheduleFetch(string bootstrapServers, IReadOnlyList<KeyValuePair<string, string>>? config = null, object? clientInstance = null)
    {
        if (string.IsNullOrEmpty(bootstrapServers))
        {
            return;
        }

        // On first call config is non-null and gets stored; on later re-fetch calls config is null
        // and GetOrAdd returns the originally stored value so auth credentials are preserved.
        var cfg = _configCache.GetOrAdd(bootstrapServers, config);

        if (_inFlight.ContainsKey(bootstrapServers))
        {
            return; // fetch already in progress
        }

        if (_cache.TryGetValue(bootstrapServers, out var existing) &&
            TickCountMs - existing.FetchedAt <= TtlMs)
        {
            return; // still fresh
        }

        // Negative-backoff: if a recent attempt did not leave us with a fresh value (initial
        // failure, or a stale entry whose refresh just failed), wait before retrying so an
        // unreachable broker is not probed on every span.
        if (_lastAttemptAt.TryGetValue(bootstrapServers, out var lastAttempt) &&
            TickCountMs - lastAttempt < RetryFloorMs)
        {
            return;
        }

        // TryAdd is the atomic guard; ContainsKey above is an optimistic fast-path only.
        if (!_inFlight.TryAdd(bootstrapServers, 0))
        {
            return; // another thread won the race
        }

        _lastAttemptAt[bootstrapServers] = TickCountMs;
        _ = Task.Run(() => FetchAndStore(bootstrapServers, cfg, clientInstance));
    }

    /// <summary>
    /// Returns the resolved cluster ID for <paramref name="bootstrapServers"/>,
    /// or <c>null</c> if no value has ever been resolved.
    /// </summary>
    internal static string? GetClusterId(string? bootstrapServers)
    {
        if (string.IsNullOrEmpty(bootstrapServers))
        {
            return null;
        }

        return _cache.TryGetValue(bootstrapServers!, out var entry) ? entry.Value : null;
    }

    /// <summary>
    /// Returns the resolved cluster ID for <paramref name="bootstrapServers"/>.
    /// If nothing has been resolved yet (e.g. the initial fetch failed or is still running),
    /// schedules a background fetch and returns <c>null</c> so the attribute becomes available
    /// on a later span. If the cached value has expired, schedules a background re-fetch and
    /// returns the stale value for this span (it stays correct unless the cluster was recreated).
    /// </summary>
    internal static string? GetClusterIdAndScheduleRefresh(string? bootstrapServers)
    {
        if (string.IsNullOrEmpty(bootstrapServers))
        {
            return null;
        }

        if (!_cache.TryGetValue(bootstrapServers!, out var entry))
        {
            // No value resolved yet — schedule a fetch so it becomes available on a later span.
            // ScheduleFetch is guarded by the in-flight and retry-floor checks, so this does not
            // hammer an unreachable broker even though it is called from the hot path.
            ScheduleFetch(bootstrapServers!);
            return null;
        }

        // TTL expired — trigger a background re-fetch, return stale value for this span.
        if (TickCountMs - entry.FetchedAt > TtlMs)
        {
            ScheduleFetch(bootstrapServers!);
        }

        return entry.Value;
    }

    private static void FetchAndStore(string bootstrapServers, IReadOnlyList<KeyValuePair<string, string>>? config, object? clientInstance)
    {
        try
        {
            var clusterId = FetchClusterIdViaReflection(bootstrapServers, config, clientInstance);
            if (!string.IsNullOrEmpty(clusterId))
            {
                _cache[bootstrapServers] = new CacheEntry(clusterId!, TickCountMs);
            }

            // On empty/failed result, keep any existing entry (serve last-known-good). The
            // cluster ID does not change unless the cluster was recreated, so a stale value is
            // preferable to none; a later scheduled fetch (after the retry floor) will retry.
        }
        catch (Exception)
        {
            // Swallow and keep any existing entry; a later scheduled fetch will retry.
        }
        finally
        {
            _inFlight.TryRemove(bootstrapServers, out _);
        }
    }

    private static string? FetchClusterIdViaReflection(string bootstrapServers, IReadOnlyList<KeyValuePair<string, string>>? extraConfig, object? clientInstance)
    {
        // Confluent.Kafka is the instrumented assembly – it is already loaded in the process.
        var kafkaAssembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, IntegrationConstants.ConfluentKafkaAssemblyName, StringComparison.Ordinal));

        if (kafkaAssembly is null)
        {
            return null;
        }

        // DescribeClusterAsync and DescribeClusterOptions only exist in Confluent.Kafka ≥ 2.x.
        // Early-exit for older versions to avoid allocating an AdminClient that cannot provide cluster ID.
        var describeClusterOptionsType = kafkaAssembly.GetType("Confluent.Kafka.Admin.DescribeClusterOptions");
        if (describeClusterOptionsType is null)
        {
            return null;
        }

        // When we have the fully-constructed client instance (producer or consumer), use
        // DependentAdminClientBuilder to share its existing rdkafka handle.  This avoids
        // allocating new rdkafka producer/consumer handles that would shift the sequential
        // client-id counters (rdkafka#producer-N / rdkafka#consumer-N) seen on spans.
        if (clientInstance is not null)
        {
            var result = TryFetchViaDependentAdmin(kafkaAssembly, clientInstance, describeClusterOptionsType);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            // Fall through to standalone AdminClient if DependentAdminClientBuilder is unavailable.
        }

        // AdminClientBuilder(IEnumerable<KeyValuePair<string,string>>) accepts a plain list directly;
        // there is no need to create an AdminClientConfig via reflection.
        var config = new List<KeyValuePair<string, string>>
        {
            new("bootstrap.servers", bootstrapServers)
        };
        if (extraConfig is not null)
        {
            foreach (var kvp in extraConfig)
            {
                if (!string.Equals(kvp.Key, "bootstrap.servers", StringComparison.OrdinalIgnoreCase))
                {
                    config.Add(kvp);
                }
            }
        }

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

        var buildMethod = adminClientBuilderType.GetMethod("Build", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        if (buildMethod is null)
        {
            return null;
        }

        var builderInstance = builderCtor.Invoke([config]);
        var adminClientInstance = buildMethod.Invoke(builderInstance, null);
        if (adminClientInstance is null)
        {
            return null;
        }

        try
        {
            return FetchFromAdminClient(adminClientInstance, describeClusterOptionsType);
        }
        finally
        {
            if (adminClientInstance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    /// Attempts to fetch the cluster ID by building an AdminClient that shares the
    /// handle of an existing producer or consumer instance via DependentAdminClientBuilder.
    /// Returns null if DependentAdminClientBuilder is not available or any step fails.
    /// </summary>
    private static string? TryFetchViaDependentAdmin(Assembly kafkaAssembly, object clientInstance, Type describeClusterOptionsType)
    {
        try
        {
            // IKafkaClientHandle duck-types the Handle property from IClient without reflection overhead.
            var handle = clientInstance.DuckCast<IKafkaClientHandle>()?.Handle;
            if (handle is null)
            {
                return null;
            }

            var dependentBuilderType = kafkaAssembly.GetType("Confluent.Kafka.DependentAdminClientBuilder");
            if (dependentBuilderType is null)
            {
                return null;
            }

            var ctor = dependentBuilderType.GetConstructor([handle.GetType()]);
            if (ctor is null)
            {
                return null;
            }

            var buildMethod = dependentBuilderType.GetMethod("Build", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (buildMethod is null)
            {
                return null;
            }

            var builder = ctor.Invoke([handle]);
            var adminClientInstance = buildMethod.Invoke(builder, null);
            if (adminClientInstance is null)
            {
                return null;
            }

            // The AdminClient shares the caller's handle and does not own it, so disposing
            // releases only admin-specific resources without closing the underlying connection.
            try
            {
                return FetchFromAdminClient(adminClientInstance, describeClusterOptionsType);
            }
            finally
            {
                if (adminClientInstance is IDisposable disposable)
                {
                    try { disposable.Dispose(); } catch { }
                }
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Fetches the cluster ID from an already-constructed admin client instance using
    /// DescribeClusterAsync (Confluent.Kafka ≥ 2.x).
    /// </summary>
    private static string? FetchFromAdminClient(object adminClientInstance, Type describeClusterOptionsType)
    {
        var adminClientType = adminClientInstance.GetType();

        // DescribeClusterAsync(DescribeClusterOptions options) → Task<DescribeClusterResult>
        var describeClusterMethod = adminClientType.GetMethod(
            "DescribeClusterAsync",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            [describeClusterOptionsType],
            null);
        if (describeClusterMethod is null)
        {
            return null;
        }

        var taskObj = describeClusterMethod.Invoke(adminClientInstance, [null]);
        if (taskObj is not Task task)
        {
            return null;
        }

        if (!task.Wait(TimeSpan.FromSeconds(10)))
        {
            return null;
        }

        var resultProp = taskObj.GetType().GetProperty("Result");
        var describeResult = resultProp?.GetValue(taskObj);
        // IDescribeClusterResult duck-types ClusterId from DescribeClusterResult without reflection.
        return describeResult?.DuckCast<IDescribeClusterResult>()?.ClusterId;
    }

    private readonly struct CacheEntry
    {
        internal CacheEntry(string value, long fetchedAt)
        {
            Value = value;
            FetchedAt = fetchedAt;
        }

        internal string Value { get; }

        internal long FetchedAt { get; }
    }
}

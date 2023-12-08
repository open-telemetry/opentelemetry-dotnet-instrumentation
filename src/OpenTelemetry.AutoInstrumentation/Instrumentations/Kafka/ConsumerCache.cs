// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka;

internal static class ConsumerCache
{
    private static readonly ConditionalWeakTable<object, string> ConsumerGroupMap = new();

    public static void Add(object consumer, string groupId)
    {
        // from the docs:
        // If the key does not exist in the table, the method invokes a callback method to create a value that is bound to the specified key.
        // AddOrUpdate not available for all of the targets
        ConsumerGroupMap.GetValue(consumer, createValueCallback: _ => groupId);
    }

    public static bool TryGet(object consumer, out string? groupId)
    {
        groupId = null;
        return ConsumerGroupMap.TryGetValue(consumer, out groupId);
    }

    public static void Remove(object instance)
    {
        ConsumerGroupMap.Remove(instance);
    }
}

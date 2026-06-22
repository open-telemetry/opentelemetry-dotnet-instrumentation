// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka;

internal static class BootstrapServersCache
{
    private static readonly ConditionalWeakTable<object, string> InstanceBootstrapServersMap = new();

    public static void Add(object instance, string bootstrapServers)
    {
        InstanceBootstrapServersMap.GetValue(instance, createValueCallback: _ => bootstrapServers);
    }

    public static bool TryGet(object instance, out string? bootstrapServers)
    {
        bootstrapServers = null;
        return InstanceBootstrapServersMap.TryGetValue(instance, out bootstrapServers);
    }

    public static void Remove(object instance)
    {
        InstanceBootstrapServersMap.Remove(instance);
    }
}

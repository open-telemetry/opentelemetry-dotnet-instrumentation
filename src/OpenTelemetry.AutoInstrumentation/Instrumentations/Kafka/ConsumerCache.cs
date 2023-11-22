// <copyright file="ConsumerCache.cs" company="OpenTelemetry Authors">
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

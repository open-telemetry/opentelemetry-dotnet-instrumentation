// <copyright file="KafkaCommon.cs" company="OpenTelemetry Authors">
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

using System.Diagnostics;
using System.Text;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka;

internal static class KafkaCommon
{
    public const string ConsumerGroupIdConfigKey = "group.id";

    public static ActivitySource Source { get; } = new("OpenTelemetry.AutoInstrumentation.Kafka");

    public static void MessageHeaderValueSetter(IKafkaMessage msg, string key, string val)
    {
        msg.Headers?.Remove(key);
        msg.Headers?.Add(key, Encoding.UTF8.GetBytes(val));
    }

    public static IEnumerable<string> MessageHeaderValueGetter(IConsumeResult? message, string key)
    {
        if (message?.Message?.Headers is not null && message.Message.Headers.TryGetLastBytes(key, out var bytes))
        {
            return new[] { Encoding.UTF8.GetString(bytes) };
        }

        return Enumerable.Empty<string>();
    }

    public static void StopCurrentConsumerActivity()
    {
        var activity = Activity.Current;
        if (activity is not null && activity.OperationName.EndsWith(MessagingAttributes.Values.ProcessOperationName, StringComparison.Ordinal))
        {
            activity.Stop();
        }
    }

    public static void SetCommonAttributes(
        Activity activity,
        string operationName,
        string? topic,
        Partition partition,
        object? key,
        IClientName? client)
    {
        activity.SetTag(MessagingAttributes.Keys.MessagingOperation, operationName);
        activity.SetTag(MessagingAttributes.Keys.MessagingSystem, MessagingAttributes.Values.KafkaMessagingSystemName);
        if (!string.IsNullOrEmpty(topic))
        {
            activity.SetTag(MessagingAttributes.Keys.DestinationName, topic);
        }

        if (client is not null)
        {
            activity.SetTag(MessagingAttributes.Keys.ClientId, client.Name);
        }

        if (key is not null)
        {
            activity.SetTag(MessagingAttributes.Keys.Kafka.MessageKey, key);
        }

        activity.SetTag(MessagingAttributes.Keys.Kafka.Partition, partition.Value);
    }
}

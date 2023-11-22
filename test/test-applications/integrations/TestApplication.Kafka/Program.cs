// <copyright file="Program.cs" company="OpenTelemetry Authors">
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

using Confluent.Kafka;

var config = new ProducerConfig { BootstrapServers = "localhost:9092" };
var topicName = args[0];
using var p = new ProducerBuilder<string, string>(config).Build();
try
{
    // produce a message without a delivery handler set
    p.Produce(topicName, new Message<string, string> { Key = "testkey", Value = "test" });
    // produce a message and set a delivery handler
    p.Produce(topicName, new Message<string, string> { Key = "testkey", Value = "test" }, report =>
    {
        Console.WriteLine($"Finished sending msg, offset: {report.Offset.Value}.");
    });
    p.Flush();
    Console.WriteLine("Delivered messages.");
}
catch (ProduceException<Null, string> e)
{
    Console.WriteLine($"Delivery failed: {e.Error.Reason}.");
}

var conf = new ConsumerConfig
{
    GroupId = $"test-consumer-group-{topicName}",
    BootstrapServers = "localhost:9092",
    // Note: The AutoOffsetReset property determines the start offset in the event
    // there are not yet any committed offsets for the consumer group for the
    // topic/partitions of interest. By default, offsets are committed
    // automatically, so in this example, consumption will only start from the
    // earliest message in the topic 'my-topic' the first time you run the program.
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = true
};

using (var consumer = new ConsumerBuilder<string, string>(conf).Build())
{
    consumer.Subscribe(topicName);

    try
    {
        ConsumeMessage(consumer);
        ConsumeMessage(consumer);
    }
    catch (ConsumeException e)
    {
        Console.WriteLine($"Error occured: {e.Error.Reason}.");
    }
}

// produce a tombstone
p.Produce(topicName, new Message<string, string> { Key = "testkey", Value = null! });

return;

void ConsumeMessage(IConsumer<string, string> consumer)
{
    var cr = consumer.Consume(TimeSpan.FromSeconds(10));
    Console.WriteLine($"Consumed message '{cr?.Message?.Value}' at: '{cr?.TopicPartitionOffset}'.");
}

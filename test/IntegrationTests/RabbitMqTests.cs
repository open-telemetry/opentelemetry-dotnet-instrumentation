// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(RabbitMqCollection.Name)]
public class RabbitMqTests : TestHelper
{
    private readonly RabbitMqFixture _rabbitMq;

    public RabbitMqTests(ITestOutputHelper output, RabbitMqFixture rabbitMq)
        : base("RabbitMq", output)
    {
        _rabbitMq = rabbitMq;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.RabbitMq), MemberType = typeof(LibraryVersion))]
    public void SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("RabbitMQ.Client.Publisher");
        collector.Expect("RabbitMQ.Client.Subscriber");

        RunTestApplication(new()
        {
            Arguments = $"--rabbitmq {_rabbitMq.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
}

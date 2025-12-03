// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using NServiceBus.Logging;

namespace TestApplication.NServiceBus;

public class TestMessageHandler : IHandleMessages<TestMessage>
{
    private static readonly ILog Log = LogManager.GetLogger<TestMessageHandler>();

    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        Log.Info("TestMessage handled");
        return Task.CompletedTask;
    }
}

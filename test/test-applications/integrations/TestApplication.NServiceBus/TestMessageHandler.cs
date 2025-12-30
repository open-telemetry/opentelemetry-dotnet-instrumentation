// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using NServiceBus.Logging;

namespace TestApplication.NServiceBus;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by NServiceBus.
internal sealed class TestMessageHandler : IHandleMessages<TestMessage>
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by NServiceBus.
{
    private static readonly ILog Log = LogManager.GetLogger<TestMessageHandler>();

    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        Log.Info("TestMessage handled");
        return Task.CompletedTask;
    }
}

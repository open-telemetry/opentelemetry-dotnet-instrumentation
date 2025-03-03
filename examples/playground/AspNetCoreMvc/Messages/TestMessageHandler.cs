// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using NServiceBus;

namespace Examples.AspNetCoreMvc.Messages;

public class TestMessageHandler : IHandleMessages<TestMessage>
{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {
        // Log or process the message here
        return Task.CompletedTask;
    }
}

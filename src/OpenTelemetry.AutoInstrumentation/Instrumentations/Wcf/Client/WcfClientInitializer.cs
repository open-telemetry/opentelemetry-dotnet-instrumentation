// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Instrumentation.Wcf;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client;

internal static class WcfClientInitializer
{
    internal interface IChannelFactory
    {
        IEndpoint Endpoint { get; }
    }

    internal interface IEndpoint
    {
        IKeyedByTypeCollection Behaviors { get; }
    }

    public static void Initialize(IChannelFactory channelFactory)
    {
        WcfInstrumentationInitializer.TryInitializeOptions();

        var behaviors = channelFactory.Endpoint.Behaviors;
        if (!behaviors.Contains(typeof(TelemetryEndpointBehavior)))
        {
            behaviors.Add(new TelemetryEndpointBehavior());
        }
    }
}

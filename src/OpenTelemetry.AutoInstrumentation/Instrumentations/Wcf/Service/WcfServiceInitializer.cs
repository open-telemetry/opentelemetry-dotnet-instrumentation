// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#if NETFRAMEWORK
using OpenTelemetry.Instrumentation.Wcf;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Service;

internal static class WcfServiceInitializer
{
    internal interface IServiceHostBase
    {
        IDescription Description { get; }
    }

    internal interface IDescription
    {
        IKeyedByTypeCollection Behaviors { get; }
    }

    public static void Initialize(IServiceHostBase serviceHost)
    {
        var behaviors = serviceHost.Description.Behaviors;
        if (!behaviors.Contains(typeof(TelemetryServiceBehavior)))
        {
            behaviors.Add(new TelemetryServiceBehavior());
        }
    }
}
#endif

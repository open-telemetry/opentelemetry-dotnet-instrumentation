// <copyright file="WcfClientInitializer.cs" company="OpenTelemetry Authors">
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
#if NETFRAMEWORK
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.Instrumentation.Wcf;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf;

internal static class WcfClientInitializer
{
    private interface IChannelFactory
    {
        IEndpoint Endpoint { get; }
    }

    private interface IEndpoint
    {
        IKeyedCollection Behaviors { get; }
    }

    private interface IKeyedCollection
    {
        void Add(object o);

        bool Contains(Type t);
    }

    public static void Initialize(object instance)
    {
        // WcfInstrumentationActivitySource.Options is initialized by WcfInitializer
        // when targeted assembly loads. Remaining work to initialize instrumentation
        // is to add telemetry behavior to the endpoint's collection.
        if (!instance.TryDuckCast<IChannelFactory>(out var channelFactory))
        {
            return;
        }

        var behaviors = channelFactory.Endpoint.Behaviors;
        if (!behaviors.Contains(typeof(TelemetryEndpointBehavior)))
        {
            behaviors.Add(new TelemetryEndpointBehavior());
        }
    }
}
#endif

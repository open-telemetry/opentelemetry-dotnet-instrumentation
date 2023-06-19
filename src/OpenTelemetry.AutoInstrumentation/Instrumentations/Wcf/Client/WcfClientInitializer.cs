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
        var behaviors = channelFactory.Endpoint.Behaviors;
        if (!behaviors.Contains(typeof(WcfClientEndpointBehavior)))
        {
            behaviors.Add(new WcfClientEndpointBehavior());
        }
    }
}
#endif

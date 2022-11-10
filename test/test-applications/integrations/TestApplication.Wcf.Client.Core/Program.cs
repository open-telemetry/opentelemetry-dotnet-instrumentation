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

using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using OpenTelemetry.Instrumentation.Wcf;
using TestApplication.Wcf.Shared;

namespace TestApplication.Wcf.Client.Core;

internal static class Program
{
    public static async Task Main()
    {
        await CallService(
            new NetTcpBinding(SecurityMode.None),
            new EndpointAddress("net.tcp://127.0.0.1:9090/Telemetry")).ConfigureAwait(false);
        await CallService(
            new BasicHttpBinding(BasicHttpSecurityMode.None),
            new EndpointAddress("http://127.0.0.1:9009/Telemetry")).ConfigureAwait(false);
    }

    private static async Task CallService(Binding binding, EndpointAddress remoteAddress)
    {
        // Note: Best practice is to re-use your client/channel instances.
        // This code is not meant to illustrate best practices, only the
        // instrumentation.
        var client = new StatusServiceClient(binding, remoteAddress);
        client.Endpoint.EndpointBehaviors.Add(new TelemetryEndpointBehavior());
        try
        {
            await client.OpenAsync().ConfigureAwait(false);

            var statusRequest = new StatusRequest
            {
                Status = Guid.NewGuid().ToString("N"),
            };

            var time = DateTimeOffset.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            var response = await client.PingAsync(
                statusRequest).ConfigureAwait(false);

            Console.WriteLine($"[{time}] Sending request with status {statusRequest.Status}. Server returned: {response?.ServerTime:yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'}");
        }
        finally
        {
            try
            {
                if (client.State == CommunicationState.Faulted)
                {
                    client.Abort();
                }
                else
                {
                    await client.CloseAsync().ConfigureAwait(false);
                }
            }
            catch
            {
            }
        }
    }
}

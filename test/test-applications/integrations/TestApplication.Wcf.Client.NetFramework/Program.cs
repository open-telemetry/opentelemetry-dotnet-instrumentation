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

using System.ServiceModel;
using System.ServiceModel.Channels;
using static System.Net.WebRequestMethods;

namespace TestApplication.Wcf.Client.NetFramework;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        string netTcpAddress;
        string httpAddress;
        if (args.Length == 0)
        {
            // Self-hosted service addresses
            netTcpAddress = "net.tcp://127.0.0.1:9090/Telemetry";
            httpAddress = "http://127.0.0.1:9009/Telemetry";
        }
        else if (args.Length == 2)
        {
            // Addresses of a service hosted in IIS inside container
            netTcpAddress = $"net.tcp://localhost:{args[0]}/StatusService.svc";
            httpAddress = $"http://localhost:{args[1]}/StatusService.svc";
        }
        else
        {
            throw new Exception("TestApplication.Wcf.Client.NetFramework application requires either 0 or exactly 2 arguments.");
        }

        await CallService(netTcpAddress, new NetTcpBinding(SecurityMode.None)).ConfigureAwait(false);
        await CallService(httpAddress, new BasicHttpBinding()).ConfigureAwait(false);
    }

    private static async Task CallService(string address, Binding binding)
    {
        // Note: Best practice is to re-use your client/channel instances.
        // This code is not meant to illustrate best practices, only the
        // instrumentation.
        var client = new StatusServiceClient(binding, new EndpointAddress(new Uri(address)));
        try
        {
            await client.OpenAsync().ConfigureAwait(false);

            var statusRequest = new StatusRequest
            {
                Status = Guid.NewGuid().ToString("N"),
            };

            var time = DateTimeOffset.UtcNow.ToString("o");
            var response = await client.PingAsync(
                statusRequest).ConfigureAwait(false);

            Console.WriteLine($"[{time}] Sending request with status {statusRequest.Status}. Server returned: {response?.ServerTime:o}");
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

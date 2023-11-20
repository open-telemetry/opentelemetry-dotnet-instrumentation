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

using System.Diagnostics;
using System.Net.Http;
#if NET8_0_OR_GREATER
using System.Threading.RateLimiting;
#endif
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
#if NET8_0_OR_GREATER
using Microsoft.AspNetCore.SignalR.Client;
#endif

namespace TestApplication.Http;

public class Program
{
    public static void Main(string[] args)
    {
        var disableDistributedContextPropagator = Environment.GetEnvironmentVariable("DISABLE_DistributedContextPropagator") == "true";
        if (disableDistributedContextPropagator)
        {
            DistributedContextPropagator.Current = DistributedContextPropagator.CreateNoOutputPropagator();
        }

        using var host = CreateHostBuilder(args).Build();
        host.Start();

        var server = (IServer?)host.Services.GetService(typeof(IServer));
        var addressFeature = server?.Features.Get<IServerAddressesFeature>();
        var address = addressFeature?.Addresses.First();
        var dnsAddress = address?.Replace("127.0.0.1", "localhost"); // needed to force DNS resolution to test metrics
        using var httpClient = new HttpClient();
        httpClient.GetAsync($"{dnsAddress}/test").Wait();
        httpClient.GetAsync($"{dnsAddress}/exception").Wait();
#if NET8_0_OR_GREATER
        var hubConnection = new HubConnectionBuilder().WithUrl($"{dnsAddress}/signalr").Build();
        hubConnection.StartAsync().Wait();
        hubConnection.StopAsync().Wait();
#endif
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
#if NET8_0_OR_GREATER
            .ConfigureServices(
                services => services
                .AddRateLimiter(rateLimiterOptions => rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext => RateLimitPartition.GetNoLimiter("1")))
                .AddConnections()
                .AddSignalR())
#endif
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls($"http://127.0.0.1:0");
            });
}

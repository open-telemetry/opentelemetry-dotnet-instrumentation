// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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

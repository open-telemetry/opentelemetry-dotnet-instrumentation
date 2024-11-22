// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Net.Http;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR.Client;

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
        httpClient.DefaultRequestHeaders.Add("Custom-Request-Test-Header1", "Test-Value1");
        httpClient.DefaultRequestHeaders.Add("Custom-Request-Test-Header2", "Test-Value2");
        httpClient.DefaultRequestHeaders.Add("Custom-Request-Test-Header3", "Test-Value3");
        httpClient.GetAsync($"{dnsAddress}/test").Wait();
        httpClient.GetAsync($"{dnsAddress}/exception").Wait();
        var hubConnection = new HubConnectionBuilder().WithUrl($"{dnsAddress}/signalr").Build();
        hubConnection.StartAsync().Wait();
        hubConnection.StopAsync().Wait();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(
                services => services
                .AddRateLimiter(rateLimiterOptions => rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext => RateLimitPartition.GetNoLimiter("1")))
                .AddConnections()
                .AddSignalR())
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls($"http://127.0.0.1:0");
            });
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Net.Http;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR.Client;
using TestApplication.Shared;

namespace TestApplication.Http;

internal static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);
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
        var dnsAddress = address?.Replace("127.0.0.1", "localhost", StringComparison.Ordinal); // needed to force DNS resolution to test metrics
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Custom-Request-Test-Header1", "Test-Value1");
        httpClient.DefaultRequestHeaders.Add("Custom-Request-Test-Header2", "Test-Value2");
        httpClient.DefaultRequestHeaders.Add("Custom-Request-Test-Header3", "Test-Value3");
        httpClient.GetAsync(new Uri($"{dnsAddress}/test")).Wait();
        httpClient.GetAsync(new Uri($"{dnsAddress}/exception")).Wait();

        // Trigger authentication metrics for .NET 10+
        httpClient.GetAsync(new Uri($"{dnsAddress}/login")).Wait();
        httpClient.GetAsync(new Uri($"{dnsAddress}/logout")).Wait();

        // Trigger authorization metrics for .NET 10+
        httpClient.GetAsync(new Uri($"{dnsAddress}/protected")).Wait();

#if NET10_0_OR_GREATER
        // Trigger Blazor Components metrics for .NET 10+
        // This will trigger Microsoft.AspNetCore.Components and Microsoft.AspNetCore.Components.Server.Circuits metrics
        httpClient.GetAsync(new Uri($"{dnsAddress}/blazor")).Wait();

        // Trigger Identity metrics for .NET 10+
        // This will trigger Microsoft.AspNetCore.Identity metrics
        httpClient.GetAsync(new Uri($"{dnsAddress}/identity/create-user")).Wait();
        httpClient.GetAsync(new Uri($"{dnsAddress}/identity/find-user")).Wait();
#endif

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

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using TestApplication.Shared;

namespace TestApplication.Logs;

internal static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("[TEST_APPLICATION_ENTRYPOINT]");
        ConsoleHelper.WriteSplashScreen(args);

        var expectedHostingStartups = ArgumentHelper.GetArgument(args, "--expected-hosting-startup-assemblies", string.Empty);
        if (expectedHostingStartups.Length > 0)
        {
            var hostingStartups = Environment.GetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES");
            if (!string.Equals(expectedHostingStartups, hostingStartups, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Expected hosting startup assemblies '{expectedHostingStartups}', got '{hostingStartups}'.");
            }
        }

        using var host = CreateHostBuilder(args).Build();
        host.Start();

        var server = (IServer?)host.Services.GetService(typeof(IServer));
        var addressFeature = server?.Features.Get<IServerAddressesFeature>();
        var address = addressFeature?.Addresses.First();
        using var httpClient = new HttpClient();
        using var response = httpClient.GetAsync(new Uri($"{address}/test")).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        Console.WriteLine("[HTTP_REQUEST_SUCCEEDED]");
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls($"http://127.0.0.1:0");
            });
}

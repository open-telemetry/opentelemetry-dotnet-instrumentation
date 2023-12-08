// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;

namespace TestApplication.Logs;

public class Program
{
    public static void Main(string[] args)
    {
        using var host = CreateHostBuilder(args).Build();
        host.Start();

        var server = (IServer?)host.Services.GetService(typeof(IServer));
        var addressFeature = server?.Features.Get<IServerAddressesFeature>();
        var address = addressFeature?.Addresses.First();
        using var httpClient = new HttpClient();
        httpClient.GetAsync($"{address}/test").Wait();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls($"http://127.0.0.1:0");
            });
}

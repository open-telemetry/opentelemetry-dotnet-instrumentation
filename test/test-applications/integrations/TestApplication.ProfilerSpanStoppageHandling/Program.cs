// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace TestApplication.ProfilerSpanStoppageHandling;

internal static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        builder.Services.AddSingleton<TestDependency>();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        using var app = builder.Build();

        // Configure the HTTP request pipeline.

        app.MapControllers();

        app.Start();

        var server = (IServer?)app.Services.GetService(typeof(IServer));
        var addressFeature = server?.Features.Get<IServerAddressesFeature>();
        var address = addressFeature?.Addresses.First();
        using var httpClient = new HttpClient();
        httpClient.Send(new HttpRequestMessage(HttpMethod.Get, $"{address}/weatherforecast"));
        // Allow for additional batch of callstacks to be collected.
        Thread.Sleep(1000);
    }
}

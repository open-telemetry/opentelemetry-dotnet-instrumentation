// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning);
        });
var logger = loggerFactory.CreateLogger<Program>();
logger.LogBeforeHostBuilt();
var builder = WebApplication.CreateBuilder(args);

using var app = builder.Build();
app.MapGet("/test", (ILogger<Program> logger) =>
{
    logger.LogRequestReceived();
    return "Hello World!";
});

app.Start();

var server = (IServer?)app.Services.GetService(typeof(IServer));
var addressFeature = server?.Features.Get<IServerAddressesFeature>();
var address = addressFeature?.Addresses.First();

using var httpClient = new HttpClient();
httpClient.GetAsync(new Uri($"{address}/test")).Wait();

internal static partial class LoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Logged before host is built.")]
    public static partial void LogBeforeHostBuilt(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request received.")]
    public static partial void LogRequestReceived(this ILogger logger);
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using CoreWCF;
using CoreWCF.Configuration;
using TestApplication.Shared;
using TestApplication.Wcf.Core;

ConsoleHelper.WriteSplashScreen(args);

var builder = WebApplication.CreateBuilder(args);

// Add CoreWCF services
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();

// Register the WCF service
builder.Services.AddSingleton<StatusService>();

// Configure Kestrel for HTTP
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(9009); // HTTP
});

// Configure NetTcp transport
builder.WebHost.UseNetTcp(9090);

var app = builder.Build();

// Configure CoreWCF
app.UseServiceModel(serviceBuilder =>
{
    // Add the StatusService
    serviceBuilder.AddService<StatusService>();

    // Configure BasicHttpBinding endpoint
    serviceBuilder.AddServiceEndpoint<StatusService, IStatusServiceContract>(
        new BasicHttpBinding(),
        "http://127.0.0.1:9009/Telemetry");

    // Configure NetTcpBinding endpoint with SecurityMode.None
    serviceBuilder.AddServiceEndpoint<StatusService, IStatusServiceContract>(
        new NetTcpBinding(SecurityMode.None),
        "net.tcp://127.0.0.1:9090/Telemetry");
});

Console.WriteLine($"[{DateTimeOffset.UtcNow:o}] Server waiting for calls");
Console.WriteLine("HTTP endpoint: http://127.0.0.1:9009/Telemetry");
Console.WriteLine("NetTcp endpoint: net.tcp://127.0.0.1:9090/Telemetry");

try
{
    await app.RunAsync().ConfigureAwait(false);
}
catch (Exception e)
{
    Console.WriteLine($"ServerException: {e}");
}

Console.WriteLine($"[{DateTimeOffset.UtcNow:o}] WCFServer: exiting main()");

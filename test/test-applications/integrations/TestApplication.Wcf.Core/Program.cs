// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using CoreWCF;
using CoreWCF.Configuration;
using TestApplication.Shared;
using TestApplication.Wcf.Core;

ConsoleHelper.WriteSplashScreen(args);

var builder = WebApplication.CreateBuilder(args);

var httpPort = GetHttpPort(args);
var tcpPort = GetTcpPort(args);

// Add CoreWCF services
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();

// Register the WCF service
builder.Services.AddSingleton<StatusService>();

// Configure Kestrel for HTTP
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(httpPort); // HTTP
});

// Configure NetTcp transport
builder.WebHost.UseNetTcp(tcpPort);

using var app = builder.Build();

var httpAddress = $"http://127.0.0.1:{httpPort}/Telemetry";
var tcpAddress = $"net.tcp://127.0.0.1:{tcpPort}/Telemetry";

// Configure CoreWCF
app.UseServiceModel(serviceBuilder =>
{
    // Add the StatusService
    serviceBuilder.AddService<StatusService>();

    // Configure BasicHttpBinding endpoint
    serviceBuilder.AddServiceEndpoint<StatusService, IStatusServiceContract>(new BasicHttpBinding(),  httpAddress);

    // Configure NetTcpBinding endpoint with SecurityMode.None
    serviceBuilder.AddServiceEndpoint<StatusService, IStatusServiceContract>(new NetTcpBinding(SecurityMode.None), $"net.tcp://127.0.0.1:{tcpPort}/Telemetry");
});

Console.WriteLine($"[{DateTimeOffset.UtcNow:o}] Server waiting for calls");
Console.WriteLine($"HTTP endpoint: {httpAddress}");
Console.WriteLine($"NetTcp endpoint: {tcpAddress}");

try
{
    await app.RunAsync().ConfigureAwait(false);
}
catch (Exception e)
{
    Console.WriteLine($"ServerException: {e}");
}

Console.WriteLine($"[{DateTimeOffset.UtcNow:o}] WCFServer: exiting main()");

return;

static int GetTcpPort(string[] args)
{
    if (args.Length > 1)
    {
        return int.Parse(args[1], CultureInfo.InvariantCulture);
    }

    return 9090;
}

static int GetHttpPort(string[] args)
{
    if (args.Length > 3)
    {
        return int.Parse(args[3], CultureInfo.InvariantCulture);
    }

    return 9009;
}

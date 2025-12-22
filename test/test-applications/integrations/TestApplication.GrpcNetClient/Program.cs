// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net;
using System.Net.Http;
using Greet;
using Grpc.Core;
using Grpc.Net.Client;
using IntegrationTests.Helpers;
#if NET
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
#endif
using TestApplication.GrpcNetClient;
#if NETFRAMEWORK
using Grpc.Net.Client.Web;
#endif
using TestApplication.Shared;

ConsoleHelper.WriteSplashScreen(args);

var port = TcpPortProvider.GetOpenPort();

#if NET
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
    serverOptions.Listen(IPAddress.Loopback, port);
});

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();

await app.StartAsync().ConfigureAwait(false);
#endif

var uri = $"http://localhost:{port}";
#if NETFRAMEWORK
var channel = GrpcChannel.ForAddress(uri, new GrpcChannelOptions
{
    HttpHandler = new GrpcWebHandler(new HttpClientHandler())
});
#else
var channel = GrpcChannel.ForAddress(uri);
#endif

var headers = new Metadata
{
    { "Custom-Request-Test-Header1", "Test-Value1" },
    { "Custom-Request-Test-Header2", "Test-Value2" },
    { "Custom-Request-Test-Header3", "Test-Value3" }
};

try
{
    var greeterClient = new Greeter.GreeterClient(channel);
    await greeterClient.SayHelloAsync(new HelloRequest { Name = "Test user" }, headers);
}
catch (RpcException e)
{
    Console.WriteLine(e);
}

#if NET
app.Lifetime.StopApplication();
#endif

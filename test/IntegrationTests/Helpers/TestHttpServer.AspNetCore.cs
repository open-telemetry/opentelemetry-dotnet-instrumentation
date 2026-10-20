// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IntegrationTests.Helpers;

internal sealed class TestHttpServer : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _name;
    private readonly IHost _listener;

    public TestHttpServer(ITestOutputHelper output, string name, params PathHandler[] pathHandlers)
        : this(
            output,
            name,
            options => options.Listen(IPAddress.Loopback, 0),
            pathHandlers)
    {
    }

    public TestHttpServer(ITestOutputHelper output, string name, X509Certificate2 serverCertificate, params PathHandler[] pathHandlers)
        : this(
            output,
            name,
            options => options.Listen(IPAddress.Any, 0, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
                listenOptions.UseHttps(serverCertificate);
            }),
            pathHandlers)
    {
    }

    private TestHttpServer(
        ITestOutputHelper output,
        string name,
        Action<KestrelServerOptions> configureKestrel,
        params PathHandler[] pathHandlers)
    {
        _output = output;
        _name = name;

        _listener = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(configureKestrel)
                    .Configure(x =>
                    {
                        foreach (var pathHandler in pathHandlers)
                        {
                            x.Map(pathHandler.Path, x =>
                            {
                                x.Run(pathHandler.Delegate);
                            });
                        }
                    })
                    .UseShutdownTimeout(TimeSpan.FromSeconds(5));
            })
            .Build();

        _listener.Start();

        var server = _listener.Services.GetRequiredService<IServer>();
        var address = server.Features
                .Get<IServerAddressesFeature>()!
                .Addresses
                .First();
        Port = int.Parse(address.Split(':').Last(), CultureInfo.InvariantCulture);
        WriteOutput($"Listening on: {string.Join(',', pathHandlers.Select(handler => $"{address}{handler.Path}"))}");
    }

    /// <summary>
    /// Gets the TCP port that this listener is listening on.
    /// </summary>
    public int Port { get; }

    public static TestHttpServer CreateDefaultTestServer(ITestOutputHelper output)
    {
        return new TestHttpServer(output, "TestDefault", new PathHandler(HandleTestRequest, "/test"));
    }

    public void Dispose()
    {
        WriteOutput("Shutting down");
        _listener.StopAsync().GetAwaiter().GetResult();
        _listener.Dispose();
    }

    private static Task HandleTestRequest(HttpContext ctx)
    {
        ctx.Response.StatusCode = 200;
        return Task.CompletedTask;
    }

    private void WriteOutput(string msg)
    {
        _output.WriteLine($"[{_name}-{nameof(TestHttpServer)}]: {msg}");
    }
}

#endif

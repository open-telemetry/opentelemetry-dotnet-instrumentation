// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class TestHttpServer : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _name;
    private readonly IWebHost _listener;

    public TestHttpServer(ITestOutputHelper output, string name, params PathHandler[] pathHandlers)
    {
        _output = output;
        _name = name;

        _listener = new WebHostBuilder()
            .UseKestrel(options =>
                options.Listen(IPAddress.Loopback, 0)) // dynamic port
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
            .Build();

        _listener.Start();

        var address = _listener.ServerFeatures!
                .Get<IServerAddressesFeature>()!
                .Addresses
                .First();
        Port = int.Parse(address.Split(':').Last());
        WriteOutput($"Listening on: {string.Join(',', pathHandlers.Select(handler => $"{address}{handler.Path}"))}");
    }

    public TestHttpServer(ITestOutputHelper output, string name, int port, params PathHandler[] pathHandlers)
    {
        _output = output;
        _name = name;
        Port = port;

        _listener = new WebHostBuilder()
            .UseKestrel(options =>
                options.Listen(IPAddress.Loopback, port))
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
            .Build();

        _listener.Start();

        var address = _listener.ServerFeatures!
                .Get<IServerAddressesFeature>()!
                .Addresses
                .First();
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
        WriteOutput($"Shutting down");
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

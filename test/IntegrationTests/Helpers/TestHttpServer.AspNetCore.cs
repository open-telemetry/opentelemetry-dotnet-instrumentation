// <copyright file="TestHttpServer.AspNetCore.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Linq;
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
    private readonly RequestDelegate _requestHandler;
    private readonly IWebHost _listener;

    public TestHttpServer(ITestOutputHelper output, RequestDelegate requestHandler, string path)
    {
        _output = output;
        _requestHandler = requestHandler;

        _listener = new WebHostBuilder()
            .UseKestrel(options =>
                options.Listen(IPAddress.Loopback, 0)) // dynamic port
            .Configure(x => x.Map(path, x =>
            {
                x.Run(requestHandler);
            }))
            .Build();

        _listener.Start();

        string address = _listener.ServerFeatures
                .Get<IServerAddressesFeature>()
                .Addresses
                .First();
        Port = int.Parse(address.Split(':').Last());
    }

    /// <summary>
    /// Gets the TCP port that this listener is listening on.
    /// </summary>
    public int Port { get; }

    public void Dispose()
    {
        WriteOutput($"Listener is shutting down.");
        _listener.Dispose();
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(TestHttpServer);
        _output.WriteLine($"[{name}]: {msg}");
    }
}

#endif

// <copyright file="Program.cs" company="OpenTelemetry Authors">
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

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace ConsoleApp.SelfBootstrap;

internal class Program
{
    private static readonly ActivitySource MyActivitySource = new ActivitySource("ConsoleApp.SelfBootstrap");
    private static TracerProvider _tracerProvider;

    private static async Task<int> Main()
    {
        var builder = Sdk
            .CreateTracerProviderBuilder()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation()
            .SetSampler(new AlwaysOnSampler())
            .AddSource("OpenTelemetry.AutoInstrumentation.*", "ConsoleApp.SelfBootstrap")
            .AddConsoleExporter()
            .AddZipkinExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
                options.ExportProcessorType = ExportProcessorType.Simple;
            });

        _tracerProvider = builder.Build();

        using (var activity = MyActivitySource.StartActivity("Main"))
        {
            activity?.SetTag("foo", "bar");

            var baseAddress = new Uri("https://www.example.com/");
            var regularHttpClient = new HttpClient { BaseAddress = baseAddress };

            await regularHttpClient.GetAsync("default-handler");
            await regularHttpClient.GetAsync("http://127.0.0.1:8080/api/mongo");
            await regularHttpClient.GetAsync("http://127.0.0.1:8080/api/redis");

            return 0;
        }
    }
}

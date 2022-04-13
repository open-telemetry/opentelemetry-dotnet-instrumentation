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

namespace Examples.ManualInstrumentations;

internal class Program
{
    private static readonly ActivitySource RegisteredActivity = new ActivitySource("Examples.ManualInstrumentations.Registered");
    private static readonly ActivitySource NonRegisteredActivity = new ActivitySource("NonRegistered.ManualInstrumentations");

    private static async Task<int> Main()
    {
        using (var activity = RegisteredActivity.StartActivity("Main"))
        {
            activity?.SetTag("foo", "bar1");
            await RunAsync();
        }

        return 0;
    }

    private static async Task RunAsync()
    {
        using (var activity = NonRegisteredActivity.StartActivity("RunAsync"))
        {
            activity?.SetTag("foo", "bar2");

            await HttpGet("https://www.example.com/default-handler");
            await HttpGet("http://127.0.0.1:8080/api/mongo");
            await HttpGet("http://127.0.0.1:8080/api/redis");
        }
    }

    private static async Task HttpGet(string url)
    {
        try
        {
            using var client = new HttpClient();
            Console.WriteLine($"Calling {url}");
            await client.GetAsync(url);
            Console.WriteLine($"Called {url}");
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"HttpRequestException occurred while calling {url}, {e}");
        }
    }
}

// <copyright file="InstrumentedHttpCall.cs" company="OpenTelemetry Authors">
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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OldReference;

public static class InstrumentedHttpCall
{

    public static async Task GetAsync(string url)
    {
        Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>> System.Diagnostics.DiagnosticSource assemblies loaded:");
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var loaded = assemblies
            .Where(assembly => assembly.FullName.Contains("System.Diagnostics.DiagnosticSource"))
            .Select(assembly => $">>>>>>>>>>>>>>>>>>>>>>> {assembly.FullName}");

        Console.WriteLine(string.Join("\n", loaded));

        var activity = new Activity("RunAsync");
        try
        {
            activity.Start();
            activity.AddTag("foo", "bar");

            using var client = new HttpClient();
            Console.WriteLine($"Calling {url}");
            await client.GetAsync(url);
            Console.WriteLine($"Called {url}");
        }
        finally
        {
            activity.Stop();
        }
    }
}

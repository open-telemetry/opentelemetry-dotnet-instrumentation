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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestApplication.Shared;

namespace TestApplication.GraphQL;

public class Program
{
    public static void Main(string[] args)
    {
        var directory = Directory.GetCurrentDirectory();

        var host = new WebHostBuilder()
            .UseKestrel(serverOptions =>
                // Explicitly set AllowSynchronousIO to true since the default changes
                // between AspNetCore 2.0 and 3.0
                serverOptions.AllowSynchronousIO = true)
            .UseContentRoot(directory)
            .UseStartup<Startup>()
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        var prefixes = new[] { "COR_", "CORECLR_", "DOTNET_", "OTEL_" };
        var envVars = from envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                      from prefix in prefixes
                      let key = (envVar.Key as string)?.ToUpperInvariant()
                      let value = envVar.Value as string
                      where key.StartsWith(prefix)
                      orderby key
                      select new KeyValuePair<string, string>(key, value);

        foreach (var kvp in envVars)
        {
            logger.LogInformation($"{kvp.Key} = {kvp.Value}");
        }

        host.Run();
    }
}

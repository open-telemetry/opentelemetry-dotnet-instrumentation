// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
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

        var envVars = ProfilerHelper.GetEnvironmentConfiguration();

        foreach (var kvp in envVars)
        {
            logger.LogInformation($"{kvp.Key} = {kvp.Value}");
        }

        host.Run();
    }
}

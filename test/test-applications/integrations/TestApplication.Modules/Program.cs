// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;
using TestApplication.Shared;

namespace TestApplication.Smoke;

internal static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        if (args.Length < 2)
        {
            throw new ArgumentException("Temp path is not provided. Use '--temp-path /my/path/to/temp_file'");
        }

        var otelLibs = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(x => x.GetName().Name)
            .Where(name => name != null && name.StartsWith("OpenTelemetry", StringComparison.Ordinal))
            .OrderBy(name => name)
            .ToList();

        var json = JsonConvert.SerializeObject(otelLibs);
        var path = args[1];

        File.WriteAllText(path, json);
    }
}

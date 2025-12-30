// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Reflection;
#endif

namespace TestApplication.MultipleAppDomains.NetFramework;

using TestApplication.Shared;
using TestLibrary.InstrumentationTarget;

internal static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        // Always execute the code to be instrumented.
        var command = new Command();
        command.Execute();

        const string NoAppDomainsSwitch = "--no-app-domains";

        if (args?.Length > 0)
        {
            if (args.Length == 1 && args[0] == NoAppDomainsSwitch)
            {
                // Nothing else to do, exit.
                return;
            }

            throw new InvalidOperationException($"Unrecognized command-line arguments: \"{string.Join(" ", args)}\"");
        }

#if NETFRAMEWORK
        const int numberOfAppDomains = 4;
        var appDomains = new AppDomain[numberOfAppDomains];
        var tasks = new List<Task>(numberOfAppDomains);
        var applicationCodeBase = Assembly.GetExecutingAssembly().CodeBase;
        for (var i = 0; i < appDomains.Length; i++)
        {
            // Use a name that contains characters that can't be used in file names.
            appDomains[i] = AppDomain.CreateDomain("|invalid:in:file:name|");
            var targetAppDomain = appDomains[i];
            tasks.Add(Task.Run(() => targetAppDomain.ExecuteAssembly(applicationCodeBase, [NoAppDomainsSwitch])));
        }

        Task.WaitAll([.. tasks]);

        for (var i = 0; i < appDomains.Length; i++)
        {
            AppDomain.Unload(appDomains[i]);
        }
#endif
    }
}

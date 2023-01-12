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

using System.Reflection;

namespace TestApplication.MultipleAppDomains.NetFramework;

using TestLibrary.InstrumentationTarget;

public static class Program
{
    public static void Main(string[] args)
    {
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
            else
            {
                throw new Exception($"Unrecognized command-line arguments: \"{string.Join(" ", args)}\"");
            }
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
            tasks.Add(Task.Run(async () =>
            {
                targetAppDomain.ExecuteAssembly(applicationCodeBase, new string[] { NoAppDomainsSwitch });

                // Ensure concurrent non-completed tasks.
                await Task.Delay(TimeSpan.FromSeconds(2.5));
            }));
        }

        Task.WaitAll(tasks.ToArray());

        for (var i = 0; i < appDomains.Length; i++)
        {
            AppDomain.Unload(appDomains[i]);
        }
#endif
    }
}

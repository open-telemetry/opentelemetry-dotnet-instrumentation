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

namespace TestApplication.DomainNeutral.NetFramework;

using TestLibrary.InstrumentationTarget;

public static class Program
{
    [LoaderOptimization(LoaderOptimization.MultiDomain)]
    public static void Main(string[] args)
    {
        var command = new Command();
        command.Execute();

        // Instrumentation assembly is expected to be already loaded from the GAC at this point.
        var instrumentationAssembly = Assembly.Load("OpenTelemetry.AutoInstrumentation") ?? throw new Exception("Instrumentation assembly was not loaded.");

#if NETFRAMEWORK
        if (!instrumentationAssembly.GlobalAssemblyCache)
        {
            throw new Exception("Instrumentation assembly was not loaded from the GAC");
        }
#endif
    }
}

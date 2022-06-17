// <copyright file="InstrumentedProcessHelper.cs" company="OpenTelemetry Authors">
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

namespace IntegrationTests.Helpers;

public class InstrumentedProcessHelper
{
    public static Process StartInstrumentedProcess(
        string executable,
        EnvironmentHelper environmentHelper,
        string arguments,
        TestSettings testSettings)
    {
        if (environmentHelper == null)
        {
            throw new ArgumentNullException(nameof(environmentHelper));
        }

        // clear all relevant environment variables to start with a clean slate
        EnvironmentHelper.ClearProfilerEnvironmentVariables();

        var startInfo = new ProcessStartInfo(executable, $"{arguments ?? string.Empty}");

        environmentHelper.SetEnvironmentVariables(testSettings, startInfo.EnvironmentVariables, executable);

        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardInput = false;

        return Process.Start(startInfo);
    }
}

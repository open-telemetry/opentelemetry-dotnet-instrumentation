// <copyright file="PowershellHelper.cs" company="OpenTelemetry Authors">
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

using System.Diagnostics;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public static class PowershellHelper
{
    public static (string StandardOutput, string ErrorOutput) RunCommand(string psCommand, ITestOutputHelper outputHelper)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = @"powershell.exe",
            Arguments = $"& {psCommand}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Verb = "runas"
        };

        var process = Process.Start(startInfo);
        var helper = new ProcessHelper(process);
        process?.WaitForExit();

        outputHelper.WriteLine($"PS> {psCommand}");
        outputHelper.WriteResult(helper);

        return (helper.StandardOutput, helper.ErrorOutput);
    }
}

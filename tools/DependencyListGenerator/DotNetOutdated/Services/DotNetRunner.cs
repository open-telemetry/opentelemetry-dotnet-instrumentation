// <copyright file="DotNetRunner.cs" company="OpenTelemetry Authors">
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
using System.Text;
using McMaster.Extensions.CommandLineUtils;

namespace DependencyListGenerator.DotNetOutdated.Services;

/// <remarks>
/// Credit for the stuff happening in here goes to the https://github.com/jaredcnance/dotnet-status project
/// </remarks>
public class DotNetRunner
{
    public RunStatus Run(string workingDirectory, string[] arguments)
    {
        var psi = new ProcessStartInfo(DotNetExe.FullPathOrDefault(), string.Join(" ", arguments))
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var p = new Process();
        try
        {
            p.StartInfo = psi;
            p.Start();

            var output = new StringBuilder();
            var errors = new StringBuilder();
            var outputTask = ConsumeStreamReaderAsync(p.StandardOutput, output);
            var errorTask = ConsumeStreamReaderAsync(p.StandardError, errors);

            var processExited = p.WaitForExit(20000);

            if (processExited == false)
            {
                p.Kill();

                return new RunStatus(output.ToString(), errors.ToString(), exitCode: -1);
            }

            Task.WaitAll(outputTask, errorTask);

            return new RunStatus(output.ToString(), errors.ToString(), p.ExitCode);
        }
        finally
        {
            p.Dispose();
        }
    }

    private static async Task ConsumeStreamReaderAsync(StreamReader reader, StringBuilder lines)
    {
        await Task.Yield();

        string line;
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
        {
            lines.AppendLine(line);
        }
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

internal static class PowershellHelper
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
        using var helper = new ProcessHelper(process);
        process?.WaitForExit();

        outputHelper.WriteLine($"PS> {psCommand}");
        outputHelper.WriteResult(helper);

        return (helper.StandardOutput, helper.ErrorOutput);
    }
}

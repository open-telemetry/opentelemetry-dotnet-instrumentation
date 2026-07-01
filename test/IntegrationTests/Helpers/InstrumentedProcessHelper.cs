// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Text;

namespace IntegrationTests.Helpers;

internal static class InstrumentedProcessHelper
{
    public static Process? Start(string executable, string? arguments, EnvironmentHelper environmentHelper)
    {
#if NET
        ArgumentNullException.ThrowIfNull(environmentHelper);
#else
        if (environmentHelper == null)
        {
            throw new ArgumentNullException(nameof(environmentHelper));
        }
#endif

        var startInfo = new ProcessStartInfo(executable, arguments ?? string.Empty);

        environmentHelper.SetEnvironmentVariables(startInfo.EnvironmentVariables);

        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardInput = false;
        startInfo.StandardOutputEncoding = Encoding.Default;

        // 1. Create the specialized process object
        var process = new IntegrationTestProcess
        {
            StartInfo = startInfo
        };

        // 2. Start it
        process.Start();

        // 3. IMMEDIATELY attach the helper.
        // This calls BeginOutputReadLine()/BeginErrorReadLine()
        // and prevents the process hang if it has a lot of output that no one reads
        process.AttachedHelper = new ProcessHelper(process);

        return process;
    }

    internal sealed class IntegrationTestProcess : Process
    {
        // This holds the helper that is already draining the process
        public ProcessHelper? AttachedHelper { get; set; }
    }
}

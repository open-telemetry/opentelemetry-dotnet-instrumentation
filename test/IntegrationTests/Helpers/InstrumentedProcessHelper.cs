// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace IntegrationTests.Helpers;

public class InstrumentedProcessHelper
{
    public static Process? Start(string executable, string? arguments, EnvironmentHelper environmentHelper)
    {
        if (environmentHelper == null)
        {
            throw new ArgumentNullException(nameof(environmentHelper));
        }

        var startInfo = new ProcessStartInfo(executable, arguments ?? string.Empty);

        environmentHelper.SetEnvironmentVariables(startInfo.EnvironmentVariables);

        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardInput = false;

        return Process.Start(startInfo);
    }
}

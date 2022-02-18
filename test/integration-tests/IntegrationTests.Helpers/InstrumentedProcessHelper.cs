using System;
using System.Diagnostics;

namespace IntegrationTests.Helpers;

public class InstrumentedProcessHelper
{
    public static Process StartInstrumentedProcess(
        string executable,
        EnvironmentHelper environmentHelper,
        string arguments = null,
        bool redirectStandardInput = false,
        int traceAgentPort = 9696,
        int aspNetCorePort = 5000,
        string processToProfile = null,
        bool enableStartupHook = true)
    {
        if (environmentHelper == null)
        {
            throw new ArgumentNullException(nameof(environmentHelper));
        }

        // clear all relevant environment variables to start with a clean slate
        EnvironmentHelper.ClearProfilerEnvironmentVariables();

        var startInfo = new ProcessStartInfo(executable, $"{arguments ?? string.Empty}");

        environmentHelper.SetEnvironmentVariables(traceAgentPort, aspNetCorePort, startInfo.EnvironmentVariables, enableStartupHook, processToProfile);

        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardInput = redirectStandardInput;

        return Process.Start(startInfo);
    }
}

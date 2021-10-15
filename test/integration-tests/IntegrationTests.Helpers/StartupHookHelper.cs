using System;
using System.Diagnostics;

namespace IntegrationTests.Helpers
{
    public class StartupHookHelper
    {
        public static Process StartProcessWithStartupHook(
            string executable,
            EnvironmentHelper environmentHelper,
            string arguments = null,
            bool redirectStandardInput = false,
            int traceAgentPort = 9696,
            int aspNetCorePort = 5000,
            int? statsdPort = null,
            string processToProfile = null)
        {
            if (environmentHelper == null)
            {
                throw new ArgumentNullException(nameof(environmentHelper));
            }

            // clear all relevant environment variables to start with a clean slate
            EnvironmentHelper.ClearProfilerEnvironmentVariables();

            var startInfo = new ProcessStartInfo(executable, $"{arguments ?? string.Empty}");

            environmentHelper.SetEnvironmentVariables(traceAgentPort, aspNetCorePort, statsdPort, startInfo.EnvironmentVariables, processToProfile, true);

            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = redirectStandardInput;

            return Process.Start(startInfo);
        }
    }
}

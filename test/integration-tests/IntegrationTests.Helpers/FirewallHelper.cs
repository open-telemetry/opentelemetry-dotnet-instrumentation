using System.Diagnostics;
using IntegrationTests.Helpers.Models;

namespace IntegrationTests.Helpers
{
    public static class FirewallHelper
    {
        public static FirewallPort OpenWinPort(int port)
        {
            string ruleName = $"TraceAgent-{port}";
            string psCommand = $"New-NetFirewallRule -DisplayName '{ruleName}' -Direction Inbound -LocalPort {port} -Protocol TCP -Action Allow";

            RunPowershell(psCommand);

            return new FirewallPort(port, ruleName);
        }

        public static void CloseWinPort(string ruleName)
        {
            string psCommand = $"Remove-NetFirewallRule -DisplayName {ruleName}";

            RunPowershell(psCommand);
        }

        private static void RunPowershell(string psCommand)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"powershell.exe";
            startInfo.Arguments = $"& {psCommand}";
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.Verb = "runas";
            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }
    }
}

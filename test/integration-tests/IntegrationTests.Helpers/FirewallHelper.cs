using System.Diagnostics;
using IntegrationTests.Helpers.Models;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers
{
    public static class FirewallHelper
    {
        public static FirewallPort OpenWinPort(int port, ITestOutputHelper output)
        {
            string ruleName = $"TraceAgent-{port}";
            string psCommand = $"New-NetFirewallRule -DisplayName '{ruleName}' -Direction Inbound -LocalPort {port} -Protocol TCP -Action Allow";

            RunPowershell(output, psCommand);

            return new FirewallPort(port, ruleName, output);
        }

        public static void CloseWinPort(string ruleName, ITestOutputHelper output)
        {
            string psCommand = $"Remove-NetFirewallRule -DisplayName {ruleName}";

            RunPowershell(output, psCommand);
        }

        private static void RunPowershell(ITestOutputHelper outputHelper, string psCommand)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"powershell.exe";
            startInfo.Arguments = $"& {psCommand}";
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.Verb = "runas";

            Process process = Process.Start(startInfo);
            ProcessHelper helper = new ProcessHelper(process);
            process.WaitForExit();

            outputHelper.WriteResult(helper);
        }
    }
}

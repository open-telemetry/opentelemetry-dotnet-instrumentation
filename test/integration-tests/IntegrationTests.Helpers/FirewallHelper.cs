using IntegrationTests.Helpers.Models;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers
{
    public static class FirewallHelper
    {
        public static FirewallPort OpenWinPort(int port, ITestOutputHelper output)
        {
            string ruleName = $"TraceAgent-{port}";
            string psCommand = $"New-NetFirewallRule -DisplayName '{ruleName}' -Direction Inbound -LocalPort {port} -Protocol TCP -Action Allow -Profile Any";

            PowershellHelper.RunCommand(psCommand, output);

            return new FirewallPort(port, ruleName, output);
        }

        public static void CloseWinPort(string ruleName, ITestOutputHelper output)
        {
            string psCommand = $"Remove-NetFirewallRule -DisplayName {ruleName}";

            PowershellHelper.RunCommand(psCommand, output);
        }
    }
}

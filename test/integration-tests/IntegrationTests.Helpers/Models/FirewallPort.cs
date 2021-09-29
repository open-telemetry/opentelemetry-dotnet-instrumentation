using System;

namespace IntegrationTests.Helpers.Models
{
    public class FirewallPort : IDisposable
    {
        public FirewallPort(int port, string rule)
        {
            Port = port;
            Rule = rule;
        }

        public int Port { get; }

        public string Rule { get; }

        public void Dispose()
        {
            FirewallHelper.CloseWinPort(Rule);
        }
    }
}

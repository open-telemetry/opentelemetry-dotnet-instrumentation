using System;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers.Models;

public class FirewallPort : IDisposable
{
    private readonly ITestOutputHelper _output;

    public FirewallPort(int port, string rule, ITestOutputHelper output)
    {
        _output = output;

        Port = port;
        Rule = rule;
    }

    public int Port { get; }

    public string Rule { get; }

    public void Dispose()
    {
        FirewallHelper.CloseWinPort(Rule, _output);
    }
}

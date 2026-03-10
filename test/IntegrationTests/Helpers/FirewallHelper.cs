// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

internal static class FirewallHelper
{
    public static FirewallPort OpenWinPort(int port, ITestOutputHelper output)
    {
        var ruleName = $"TraceAgent-{port}";
        var psCommand = $"New-NetFirewallRule -DisplayName '{ruleName}' -Direction Inbound -LocalPort {port} -Protocol TCP -Action Allow -Profile Any";

        PowershellHelper.RunCommand(psCommand, output);

        return new FirewallPort(port, ruleName, output);
    }

    public static void CloseWinPort(string ruleName, ITestOutputHelper output)
    {
        var psCommand = $"Remove-NetFirewallRule -DisplayName {ruleName}";

        PowershellHelper.RunCommand(psCommand, output);
    }
}

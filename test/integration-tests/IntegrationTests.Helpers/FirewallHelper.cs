// <copyright file="FirewallHelper.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using IntegrationTests.Helpers.Models;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

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

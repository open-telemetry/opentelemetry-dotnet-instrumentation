// <copyright file="FirewallPort.cs" company="OpenTelemetry Authors">
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

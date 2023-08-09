// <copyright file="StringConfigurationSourceTests.cs" company="OpenTelemetry Authors">
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

using FluentAssertions;
using OpenTelemetry.AutoInstrumentation.Configurations;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

public class StringConfigurationSourceTests
{
    [Fact]
    public void GetBool_FailFast()
    {
        var stringConfigurationSource = new StringConfigurationSourceImplementation(true, "non-bool");

        var action = () => stringConfigurationSource.GetBool("anyKey");

        action.Should().Throw<FormatException>();
    }

    [Fact]
    public void GetDouble_FailFast()
    {
        var stringConfigurationSource = new StringConfigurationSourceImplementation(true, "non-double");

        var action = () => stringConfigurationSource.GetDouble("anyKey");

        action.Should().Throw<FormatException>();
    }

    [Fact]
    public void GetInt32_FailFast()
    {
        var stringConfigurationSource = new StringConfigurationSourceImplementation(true, "non-int");

        var action = () => stringConfigurationSource.GetInt32("anyKey");

        action.Should().Throw<FormatException>();
    }

    private class StringConfigurationSourceImplementation : StringConfigurationSource
    {
        private readonly string _returnedValue;

        public StringConfigurationSourceImplementation(bool failFast, string returnedValue)
            : base(failFast)
        {
            _returnedValue = returnedValue;
        }

        public override string? GetString(string key)
        {
            return _returnedValue;
        }
    }
}

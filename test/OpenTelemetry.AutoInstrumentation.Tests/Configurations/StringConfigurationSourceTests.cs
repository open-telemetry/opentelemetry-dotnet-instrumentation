// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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

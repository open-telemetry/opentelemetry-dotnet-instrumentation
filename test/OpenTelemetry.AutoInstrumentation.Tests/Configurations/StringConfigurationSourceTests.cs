// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

public class StringConfigurationSourceTests
{
    [Fact]
    public void GetBool_FailFast()
    {
        var stringConfigurationSource = new StringConfigurationSourceImplementation(true, "non-bool");

        Assert.Throws<FormatException>(() => stringConfigurationSource.GetBool("anyKey"));
    }

    [Fact]
    public void GetDouble_FailFast()
    {
        var stringConfigurationSource = new StringConfigurationSourceImplementation(true, "non-double");

        Assert.Throws<FormatException>(() => stringConfigurationSource.GetDouble("anyKey"));
    }

    [Fact]
    public void GetInt32_FailFast()
    {
        var stringConfigurationSource = new StringConfigurationSourceImplementation(true, "non-int");

        Assert.Throws<FormatException>(() => stringConfigurationSource.GetInt32("anyKey"));
    }

    private sealed class StringConfigurationSourceImplementation : StringConfigurationSource
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

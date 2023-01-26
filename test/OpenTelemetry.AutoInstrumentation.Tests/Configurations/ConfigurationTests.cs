// <copyright file="ConfigurationTests.cs" company="OpenTelemetry Authors">
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

using System.Collections.Specialized;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using OpenTelemetry.AutoInstrumentation.Configurations;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

public class ConfigurationTests
{
    private enum TestEnum
    {
        Test1,
        Test2,
        Test3
    }

    [Fact]
    public void ParseEnabledEnumList_Default()
    {
        var source = new Configuration(new NameValueConfigurationSource(new NameValueCollection()));

        var list = source.ParseEnabledEnumList<TestEnum>(
            enabledConfiguration: "TEST_ENABLED_VALUES",
            disabledConfiguration: "TEST_DISABLED_VALUES",
            error: "Invalid enum value: {0}");

        list.Should().Equal(TestEnum.Test1, TestEnum.Test2, TestEnum.Test3);
    }

    [Fact]
    public void ParseEnabledEnumList_Enabled()
    {
        var source = new Configuration(new NameValueConfigurationSource(new NameValueCollection()
        {
            { "TEST_ENABLED_VALUES", "Test1,Test3" }
        }));

        var list = source.ParseEnabledEnumList<TestEnum>(
            enabledConfiguration: "TEST_ENABLED_VALUES",
            disabledConfiguration: "TEST_DISABLED_VALUES",
            error: "Invalid enum value: {0}");

        list.Should().Equal(TestEnum.Test1, TestEnum.Test3);
    }

    [Fact]
    public void ParseEnabledEnumList_Disabled()
    {
        var source = new Configuration(new NameValueConfigurationSource(new NameValueCollection()
        {
            { "TEST_DISABLED_VALUES", "Test2" }
        }));

        var list = source.ParseEnabledEnumList<TestEnum>(
            enabledConfiguration: "TEST_ENABLED_VALUES",
            disabledConfiguration: "TEST_DISABLED_VALUES",
            error: "Invalid enum value: {0}");

        list.Should().Equal(TestEnum.Test1, TestEnum.Test3);
    }

    [Fact]
    public void ParseEnabledEnumList_None()
    {
        var source = new Configuration(new NameValueConfigurationSource(new NameValueCollection()
        {
            { "TEST_ENABLED_VALUES", "none" }
        }));

        var list = source.ParseEnabledEnumList<TestEnum>(
            enabledConfiguration: "TEST_ENABLED_VALUES",
            disabledConfiguration: "TEST_DISABLED_VALUES",
            error: "Invalid enum value: {0}");

        list.Should().BeEmpty();
    }

    [Fact]
    public void ParseEnabledEnumList_Invalid()
    {
        var source = new Configuration(new NameValueConfigurationSource(new NameValueCollection()
        {
            { "TEST_ENABLED_VALUES", "invalid" }
        }));

        var act = () => source.ParseEnabledEnumList<TestEnum>(
            enabledConfiguration: "TEST_ENABLED_VALUES",
            disabledConfiguration: "TEST_DISABLED_VALUES",
            error: "Invalid enum value: {0}");

        act.Should().Throw<FormatException>()
            .WithMessage("Invalid enum value: invalid");
    }

    [Fact]
    public void ParseDisabledEnumList_Invalid()
    {
        var source = new Configuration(new NameValueConfigurationSource(new NameValueCollection()
        {
            { "TEST_DISABLED_VALUES", "invalid" }
        }));

        var act = () => source.ParseEnabledEnumList<TestEnum>(
            enabledConfiguration: "TEST_ENABLED_VALUES",
            disabledConfiguration: "TEST_DISABLED_VALUES",
            error: "Invalid enum value: {0}");

        act.Should().Throw<FormatException>()
            .WithMessage("Invalid enum value: invalid");
    }

    [Fact]
    public void ParseEmptyAsNull_CompositeConfigurationSource()
    {
        var mockSource = new Mock<IConfigurationSource>();
        mockSource.Setup(x => x.GetString(It.Is<string>(x => x == "TEST_NULL_VALUE"))).Returns<string>(k => null);
        mockSource.Setup(x => x.GetString(It.Is<string>(x => x == "TEST_EMPTY_VALUE"))).Returns<string>(k => string.Empty);
        var compositeSource = new Configuration(mockSource.Object);

        using (new AssertionScope())
        {
            compositeSource.GetString("TEST_NULL_VALUE").Should().BeNull();
            compositeSource.GetString("TEST_EMPTY_VALUE").Should().BeNull();
        }
    }
}

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
    public void ParseEnabledEnumList_Default_Enabled()
    {
        var source = new Configuration(new NameValueConfigurationSource(new NameValueCollection()));

        var list = source.ParseEnabledEnumList<TestEnum>(
            enabledByDefault: true,
            enabledConfigurationTemplate: "TEST_CONFIGURATION_{0}_ENABLED");

        list.Should().Equal(TestEnum.Test1, TestEnum.Test2, TestEnum.Test3);
    }

    [Fact]
    public void ParseEnabledEnumList_Default_Disabled()
    {
        var source = new Configuration(new NameValueConfigurationSource(new NameValueCollection()));

        var list = source.ParseEnabledEnumList<TestEnum>(
            enabledByDefault: false,
            enabledConfigurationTemplate: "TEST_CONFIGURATION_{0}_ENABLED");

        list.Should().BeEmpty();
    }

    [Fact]
    public void ParseEnabledEnumList_SelectivelyEnabled()
    {
        var source = new Configuration(new NameValueConfigurationSource(new NameValueCollection
        {
            { "TEST_CONFIGURATION_Test1_ENABLED", "true" },
            { "TEST_CONFIGURATION_Test3_ENABLED", "true" }
        }));

        var list = source.ParseEnabledEnumList<TestEnum>(
            enabledByDefault: false,
            enabledConfigurationTemplate: "TEST_CONFIGURATION_{0}_ENABLED");

        list.Should().Equal(TestEnum.Test1, TestEnum.Test3);
    }

    [Fact]
    public void ParseEnabledEnumList_SelectivelyDisabled()
    {
        var source = new Configuration(new NameValueConfigurationSource(new NameValueCollection
        {
            { "TEST_CONFIGURATION_Test2_ENABLED", "false" },
        }));

        var list = source.ParseEnabledEnumList<TestEnum>(
            enabledByDefault: true,
            enabledConfigurationTemplate: "TEST_CONFIGURATION_{0}_ENABLED");

        list.Should().Equal(TestEnum.Test1, TestEnum.Test3);
    }

    [Fact]
    public void ParseEmptyAsNull_CompositeConfigurationSource()
    {
        var mockSource = new Mock<IConfigurationSource>();
        mockSource.Setup(x => x.GetString(It.Is<string>(key => key == "TEST_NULL_VALUE"))).Returns<string>(_ => null);
        mockSource.Setup(x => x.GetString(It.Is<string>(key => key == "TEST_EMPTY_VALUE"))).Returns<string>(_ => string.Empty);
        var compositeSource = new Configuration(mockSource.Object);

        using (new AssertionScope())
        {
            compositeSource.GetString("TEST_NULL_VALUE").Should().BeNull();
            compositeSource.GetString("TEST_EMPTY_VALUE").Should().BeNull();
        }
    }
}

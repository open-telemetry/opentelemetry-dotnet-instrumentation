// <copyright file="ConfigurationSourceExtensionsTests.cs" company="OpenTelemetry Authors">
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
using System.Collections.Specialized;
using FluentAssertions;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.AutoInstrumentation.Util;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Util
{
    public class ConfigurationSourceExtensionsTests
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
            var source = new NameValueConfigurationSource(new NameValueCollection());

            var list = source.ParseEnabledEnumList<TestEnum>(
                enabledConfiguration: "TEST_ENABLED_VALUES",
                disabledConfiguration: "TEST_DISABLED_VALUES",
                separator: ',',
                error: "Invalid enum value: {0}");

            list.Should().Equal(TestEnum.Test1, TestEnum.Test2, TestEnum.Test3);
        }

        [Fact]
        public void ParseEnabledEnumList_Enabled()
        {
            var source = new NameValueConfigurationSource(new NameValueCollection()
            {
                { "TEST_ENABLED_VALUES", "Test1,Test3" }
            });

            var list = source.ParseEnabledEnumList<TestEnum>(
                enabledConfiguration: "TEST_ENABLED_VALUES",
                disabledConfiguration: "TEST_DISABLED_VALUES",
                separator: ',',
                error: "Invalid enum value: {0}");

            list.Should().Equal(TestEnum.Test1, TestEnum.Test3);
        }

        [Fact]
        public void ParseEnabledEnumList_Disabled()
        {
            var source = new NameValueConfigurationSource(new NameValueCollection()
            {
                { "TEST_DISABLED_VALUES", "Test2" }
            });

            var list = source.ParseEnabledEnumList<TestEnum>(
                enabledConfiguration: "TEST_ENABLED_VALUES",
                disabledConfiguration: "TEST_DISABLED_VALUES",
                separator: ',',
                error: "Invalid enum value: {0}");

            list.Should().Equal(TestEnum.Test1, TestEnum.Test3);
        }

        [Fact]
        public void ParseEnabledEnumList_None()
        {
            var source = new NameValueConfigurationSource(new NameValueCollection()
            {
                { "TEST_ENABLED_VALUES", "none" }
            });

            var list = source.ParseEnabledEnumList<TestEnum>(
                enabledConfiguration: "TEST_ENABLED_VALUES",
                disabledConfiguration: "TEST_DISABLED_VALUES",
                separator: ',',
                error: "Invalid enum value: {0}");

            list.Should().BeEmpty();
        }

        [Fact]
        public void ParseEnabledEnumList_Invalid()
        {
            var source = new NameValueConfigurationSource(new NameValueCollection()
            {
                { "TEST_ENABLED_VALUES", "invalid" }
            });

            var act = () => source.ParseEnabledEnumList<TestEnum>(
                enabledConfiguration: "TEST_ENABLED_VALUES",
                disabledConfiguration: "TEST_DISABLED_VALUES",
                separator: ',',
                error: "Invalid enum value: {0}");

            act.Should().Throw<FormatException>()
               .WithMessage("Invalid enum value: invalid");
        }
    }
}

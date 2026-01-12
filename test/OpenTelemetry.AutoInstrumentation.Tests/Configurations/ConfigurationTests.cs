// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
#if NET
using System.Text;
#endif
using NSubstitute;
using OpenTelemetry.AutoInstrumentation.Configurations;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations;

public class ConfigurationTests
{
#if NET
    private static readonly CompositeFormat TestConfigurationTemplate = CompositeFormat.Parse("TEST_CONFIGURATION_{0}_ENABLED");
#else
    private const string TestConfigurationTemplate = "TEST_CONFIGURATION_{0}_ENABLED";
#endif

    private enum TestEnum
    {
        Test1,
        Test2,
        Test3
    }

    [Fact]
    public void ParseEnabledEnumList_Default_Enabled()
    {
        var source = new Configuration(false, new NameValueConfigurationSource(false, new NameValueCollection()));

        var list = source.ParseEnabledEnumList<TestEnum>(
            enabledByDefault: true,
            enabledConfigurationTemplate: TestConfigurationTemplate);

        Assert.Equal([TestEnum.Test1, TestEnum.Test2, TestEnum.Test3], list);
    }

    [Fact]
    public void ParseEnabledEnumList_Default_Disabled()
    {
        var source = new Configuration(false, new NameValueConfigurationSource(false, new NameValueCollection()));

        var list = source.ParseEnabledEnumList<TestEnum>(
            enabledByDefault: false,
            enabledConfigurationTemplate: TestConfigurationTemplate);

        Assert.Empty(list);
    }

    [Fact]
    public void ParseEnabledEnumList_SelectivelyEnabled()
    {
        var source = new Configuration(false, new NameValueConfigurationSource(false, new NameValueCollection
        {
            { "TEST_CONFIGURATION_TEST1_ENABLED", "true" },
            { "TEST_CONFIGURATION_TEST3_ENABLED", "true" }
        }));

        var list = source.ParseEnabledEnumList<TestEnum>(
            enabledByDefault: false,
            enabledConfigurationTemplate: TestConfigurationTemplate);

        Assert.Equal([TestEnum.Test1, TestEnum.Test3], list);
    }

    [Fact]
    public void ParseEnabledEnumList_SelectivelyDisabled()
    {
        var source = new Configuration(false, new NameValueConfigurationSource(false, new NameValueCollection
        {
            { "TEST_CONFIGURATION_TEST2_ENABLED", "false" },
        }));

        var list = source.ParseEnabledEnumList<TestEnum>(
            enabledByDefault: true,
            enabledConfigurationTemplate: TestConfigurationTemplate);

        Assert.Equal([TestEnum.Test1, TestEnum.Test3], list);
    }

    [Fact]
    public void ParseEnabledEnumList_WrongValue()
    {
        var source = new Configuration(false, new NameValueConfigurationSource(false, new NameValueCollection
        {
            { "TEST_CONFIGURATION_TEST2_ENABLED", "WrongValue" },
        }));

        var list = source.ParseEnabledEnumList<TestEnum>(
            enabledByDefault: true,
            enabledConfigurationTemplate: TestConfigurationTemplate);

        Assert.Equal([TestEnum.Test1, TestEnum.Test2, TestEnum.Test3], list);
    }

    [Fact]
    public void ParseEnabledEnumList_WrongValue_FailFast()
    {
        var source = new Configuration(true, new NameValueConfigurationSource(true, new NameValueCollection
        {
            { "TEST_CONFIGURATION_TEST2_ENABLED", "WrongValue" },
        }));

        Assert.Throws<FormatException>(() => source.ParseEnabledEnumList<TestEnum>(
            enabledByDefault: true,
            enabledConfigurationTemplate: TestConfigurationTemplate));
    }

    [Fact]
    public void ParseEnabledEnumList_ParseEmptyAsNull_CompositeConfigurationSource()
    {
        var mockSource = Substitute.For<IConfigurationSource>();
        mockSource.GetString(Arg.Is<string>(key => key == "TEST_NULL_VALUE")).Returns(_ => null);
        mockSource.GetString(Arg.Is<string>(key => key == "TEST_EMPTY_VALUE"))!.Returns<string>(_ => string.Empty);
        var compositeSource = new Configuration(true, mockSource);

        Assert.Null(compositeSource.GetString("TEST_NULL_VALUE"));
        Assert.Null(compositeSource.GetString("TEST_EMPTY_VALUE"));
    }

    [Fact]
    public void ParseList_ParseSingleElement()
    {
        var source = new Configuration(false, new NameValueConfigurationSource(false, new NameValueCollection
        {
            { "TEST_LIST", "Value1" },
        }));

        var list = source.ParseList("TEST_LIST", ',');

        Assert.Equal(["Value1"], list);
    }

    [Fact]
    public void ParseList_ParseMultipleList()
    {
        var source = new Configuration(false, new NameValueConfigurationSource(false, new NameValueCollection
        {
            { "TEST_LIST", "Value1,Value2" },
        }));

        var list = source.ParseList("TEST_LIST", ',');

        Assert.Equal(["Value1", "Value2"], list);
    }

    [Fact]
    public void ParseList_ParseEmptyEntry()
    {
        var source = new Configuration(false, new NameValueConfigurationSource(false, new NameValueCollection
        {
            { "TEST_LIST", "Value1,,Value2" },
        }));

        var list = source.ParseList("TEST_LIST", ',');

        Assert.Equal(["Value1", "Value2"], list);
    }

    [Fact]
    public void ParseList_ParseNullAsEmpty()
    {
        var source = new Configuration(false, new NameValueConfigurationSource(false, new NameValueCollection()));

        var list = source.ParseList("TEST_LIST", ',');

        Assert.Empty(list);
    }
}

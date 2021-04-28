using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Datadog.Trace.Configuration;
using Datadog.Trace.Util;
using Xunit;

namespace Datadog.Trace.Tests.Configuration
{
    public class ConfigurationSourceExtensionsTests
    {
        public enum TestEnum
        {
            /// <summary>
            /// Test value 1
            /// </summary>
            TestValue1,

            /// <summary>
            /// Test value 2
            /// </summary>
            TestValue2,

            /// <summary>
            /// Test value 3
            /// </summary>
            TestValue3,
        }

        public static IEnumerable<object[]> GetEnumData()
        {
            yield return new object[] { new NameValueCollection { { "example1", "value1" } }, "example2", default(TestEnum) };
            yield return new object[] { new NameValueCollection { { "example1", "value1" } }, "example1", default(TestEnum) };
            yield return new object[] { new NameValueCollection { { "example1", "TestValue2" } }, "example1", TestEnum.TestValue2 };
        }

        public static IEnumerable<object[]> GetEnumsData()
        {
            yield return new object[] { new NameValueCollection { { "example1", "value1" } }, "example2", ArrayHelper.Empty<TestEnum>() };
            yield return new object[] { new NameValueCollection { { "example1", "value1" } }, "example1", ArrayHelper.Empty<TestEnum>() };
            yield return new object[] { new NameValueCollection { { "example1", "TestValue2,TestValue3,," } }, "example1", new[] { TestEnum.TestValue2, TestEnum.TestValue3 } };
        }

        [Fact]
        public void GetStrings_CanHandleNullSource()
        {
            var result = ConfigurationSourceExtensions.GetStrings(null, "test");

            Assert.Same(Enumerable.Empty<string>(), result);
        }

        [Fact]
        public void GetString_NoMatches()
        {
            var collection = new NameValueCollection
            {
                { "example1", "value1" },
                { "example2", "value2" }
            };
            var cs = new NameValueConfigurationSource(collection);
            var result = ConfigurationSourceExtensions.GetStrings(cs, "test");

            Assert.Empty(result);
        }

        [Fact]
        public void GetString_MultipleValues()
        {
            var collection = new NameValueCollection
            {
                { "example1", "value1,value2,," }
            };
            var cs = new NameValueConfigurationSource(collection);
            var result = ConfigurationSourceExtensions.GetStrings(cs, "example1");

            Assert.Contains(result, (v) => v == "value1");
            Assert.Contains(result, (v) => v == "value2");
            Assert.True(result.Count() == 2);
        }

        [Fact]
        public void GetTypedValue_CanHandleNullSource()
        {
            var result = ConfigurationSourceExtensions.GetTypedValue<TestEnum>(null, "example2");

            Assert.Equal(default(TestEnum), result);
        }

        [Theory]
        [MemberData(nameof(GetEnumData))]
        public void GetTypedValue(NameValueCollection collection, string key, TestEnum expected)
        {
            var cs = new NameValueConfigurationSource(collection);
            var result = ConfigurationSourceExtensions.GetTypedValue<TestEnum>(cs, key);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetTypedValues_CanHandleNullSource()
        {
            var result = ConfigurationSourceExtensions.GetTypedValues<TestEnum>(null, "example2");

            Assert.True(result.Count() == 0);
        }

        [Theory]
        [MemberData(nameof(GetEnumsData))]
        public void GetTypedValues(NameValueCollection collection, string key, TestEnum[] expected)
        {
            var cs = new NameValueConfigurationSource(collection);
            var result = ConfigurationSourceExtensions.GetTypedValues<TestEnum>(cs, key);

            Assert.Equal(expected, result);
        }
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using Vendors.YamlDotNet.Core;
using Vendors.YamlDotNet.Serialization;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBased;

internal static class FileBasedTestHelper
{
    public static void MoveParserToScalar(IParser parser)
    {
        parser.MoveNext(); // StreamStart
        parser.MoveNext(); // DocumentStart
        parser.MoveNext(); // Scalar
    }

    public static void AssertAliasPropertyExists<T>(T obj, string alias)
    {
        Assert.NotNull(obj);

        var prop = typeof(T).GetProperties()
                            .FirstOrDefault(p => p.GetCustomAttribute<YamlMemberAttribute>()?.Alias == alias);

        Assert.NotNull(prop);

        var value = prop.GetValue(obj);
        Assert.NotNull(value);
    }

    public static void AssertCountOfAliasProperties<T>(T obj, int expectedCount)
    {
        Assert.NotNull(obj);

        var props = typeof(T).GetProperties()
                            .Where(p => p.GetCustomAttribute<YamlMemberAttribute>()?.Alias != null)
                            .ToList();

        Assert.Equal(expectedCount, props.Count);
    }
}

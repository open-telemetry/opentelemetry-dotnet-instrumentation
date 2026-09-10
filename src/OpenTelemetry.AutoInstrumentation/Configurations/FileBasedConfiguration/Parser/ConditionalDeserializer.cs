// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Core;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;

internal class ConditionalDeserializer : INodeDeserializer
{
    private readonly INodeDeserializer _inner;

    public ConditionalDeserializer(INodeDeserializer inner)
    {
        this._inner = inner;
    }

    public bool Deserialize(
         IParser reader,
         Type expectedType,
         Func<IParser, Type, object?> nestedObjectDeserializer,
         out object? value,
         ObjectDeserializer rootDeserializer)
    {
        var isEmptyObjectOnEmptyYamlAttribute = expectedType.CustomAttributes.Any(a => string.Equals(a.AttributeType.Name, "EmptyObjectOnEmptyYamlAttribute", StringComparison.Ordinal));

        if (!isEmptyObjectOnEmptyYamlAttribute)
        {
            return _inner.Deserialize(reader, expectedType, nestedObjectDeserializer, out value, rootDeserializer);
        }

        var result = _inner.Deserialize(reader, expectedType, nestedObjectDeserializer, out value, rootDeserializer);

        if (result && value is null)
        {
            value = Activator.CreateInstance(expectedType);
        }

        return result;
    }
}

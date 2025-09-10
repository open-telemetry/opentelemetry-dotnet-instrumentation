// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using Vendors.YamlDotNet.Core;
using Vendors.YamlDotNet.Core.Events;
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
        if (!expectedType.IsDefined(typeof(EmptyObjectOnEmptyYamlAttribute), inherit: true))
        {
            return _inner.Deserialize(reader, expectedType, nestedObjectDeserializer, out value, rootDeserializer);
        }

        if (reader.Accept<Scalar>(out var scalar))
        {
            if (string.IsNullOrEmpty(scalar.Value))
            {
                value = Activator.CreateInstance(expectedType);
                reader.MoveNext();
                return true;
            }
        }

        if (reader.Accept<MappingStart>(out _))
        {
            object tempValue = Activator.CreateInstance(expectedType)!;

            bool result = _inner.Deserialize(reader, expectedType, nestedObjectDeserializer, out var deserialized, rootDeserializer);

            value = deserialized ?? tempValue;
            return result;
        }

        value = null;
        return false;
    }
}

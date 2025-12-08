// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Text.RegularExpressions;
using Vendors.YamlDotNet.Core;
using Vendors.YamlDotNet.Core.Events;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;

internal partial class EnvVarTypeConverter : IYamlTypeConverter
{
    private static readonly HashSet<Type> SupportedTypes =
    [
        typeof(string),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(float),
        typeof(double),
        typeof(bool),
        typeof(int?),
        typeof(uint?),
        typeof(object),
    ];

    public bool Accepts(Type type)
    {
        return SupportedTypes.Contains(type);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.Current is not Scalar scalar)
        {
            return rootDeserializer(type);
        }

        var rawValue = scalar.Value ?? throw new InvalidOperationException("Scalar value is null.");

        var replacedValue = ReplaceEnvVariables(rawValue);
        parser.MoveNext();

        try
        {
            if (string.IsNullOrWhiteSpace(replacedValue))
            {
                if (Nullable.GetUnderlyingType(type) != null)
                {
                    return null;
                }
            }

            return type switch
            {
                Type t when t == typeof(string) => replacedValue,
                Type t when t == typeof(int) => int.Parse(replacedValue),
                Type t when t == typeof(uint) => uint.Parse(replacedValue),
                Type t when t == typeof(long) => long.Parse(replacedValue),
                Type t when t == typeof(float) => float.Parse(replacedValue, CultureInfo.InvariantCulture),
                Type t when t == typeof(double) => double.Parse(replacedValue, CultureInfo.InvariantCulture),
                Type t when t == typeof(bool) => bool.Parse(replacedValue),
                Type t when t == typeof(int?) => int.Parse(replacedValue),
                Type t when t == typeof(uint?) => uint.Parse(replacedValue),
                Type t when t == typeof(object) => replacedValue,
                _ => throw new NotSupportedException($"Type {type.FullName} is not supported by the converter")
            };
        }
        catch (Exception ex)
        {
            throw new FormatException($"Error parsing value '{replacedValue}' as type {type.Name}", ex);
        }
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        emitter.Emit(new Scalar(value?.ToString() ?? string.Empty));
    }

    private static string ReplaceEnvVariables(string input)
    {
        return GetEnvVarRegex().Replace(input, match =>
        {
            var varName = match.Groups[1].Value;
            var fallback = match.Groups[2].Success ? match.Groups[2].Value : null;
            var envValue = Environment.GetEnvironmentVariable(varName);
            return envValue ?? fallback ?? match.Value;
        });
    }

#if NET
    [GeneratedRegex(@"\$\{([A-Z0-9_]+)(?::-([^}]*))?\}", RegexOptions.Compiled)]
    private static partial Regex GetEnvVarRegex();

#else

#pragma warning disable SA1201 // A field should not follow a method
    private static readonly Regex EnvVarRegex = new(@"\$\{([A-Z0-9_]+)(?::-([^}]*))?\}", RegexOptions.Compiled);
#pragma warning restore SA1201 // A field should not follow a method

    private static Regex GetEnvVarRegex() => EnvVarRegex;
#endif
}

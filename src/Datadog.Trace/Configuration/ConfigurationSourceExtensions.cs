using System;
using System.Collections.Generic;
using System.Linq;

namespace Datadog.Trace.Configuration
{
    internal static class ConfigurationSourceExtensions
    {
        private const char Splitter = ';';

        public static IEnumerable<string> GetStrings(this IConfigurationSource source, string key)
        {
            return source?.GetString(key)
                ?.Split(new[] { Splitter }, StringSplitOptions.RemoveEmptyEntries)
                ?? Enumerable.Empty<string>();
        }

        public static TEnum GetTypedValue<TEnum>(this IConfigurationSource source, string key)
            where TEnum : struct, IConvertible
        {
            Enum.TryParse(source?.GetString(key) ?? "default", ignoreCase: true, out TEnum typedValue);
            return typedValue;
        }

        public static HashSet<TEnum> GetTypedValues<TEnum>(this IConfigurationSource source, string key)
            where TEnum : struct, IConvertible
        {
            var values = GetStrings(source, key);

            if (!values.Any())
            {
                return new HashSet<TEnum>() { default };
            }

            var typedValues = GetTypedValues<TEnum>(values);

            if (!typedValues.Any())
            {
                return new HashSet<TEnum>() { default };
            }

            return new HashSet<TEnum>(typedValues);
        }

        private static IEnumerable<TEnum> GetTypedValues<TEnum>(IEnumerable<string> values)
            where TEnum : struct, IConvertible
        {
            foreach (string value in values)
            {
                if (Enum.TryParse(value, ignoreCase: true, out TEnum typedValue))
                {
                    yield return typedValue;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Datadog.Trace.Configuration
{
    internal static class ConfigurationSourceExtensions
    {
        private const char Separator = ';';

        public static IEnumerable<string> GetStrings(this IConfigurationSource source, string key)
        {
            return source?.GetString(key)
                ?.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries)
                ?? Enumerable.Empty<string>();
        }

        public static TEnum GetTypedValue<TEnum>(this IConfigurationSource source, string key)
            where TEnum : struct, IConvertible
        {
            Enum.TryParse(source?.GetString(key), ignoreCase: true, out TEnum typedValue);
            return typedValue;
        }

        public static IEnumerable<TEnum> GetTypedValues<TEnum>(this IConfigurationSource source, string key)
            where TEnum : struct, IConvertible
        {
            var values = GetStrings(source, key);

            if (values.Any())
            {
                foreach (var item in GetTypedValues<TEnum>(values))
                {
                    yield return item;
                }
            }
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

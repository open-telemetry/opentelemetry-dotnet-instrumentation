using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Nuke.Common;
using Nuke.Common.Tooling;

[TypeConverter(typeof(TargetFrameworkTypeConverter))]
public class TargetFramework : Enumeration
{
    public static readonly TargetFramework NET462 = new() { Value = "net462" };
    public static readonly TargetFramework NETCOREAPP3_1 = new() { Value = "netcoreapp3.1" };
    public static readonly TargetFramework NET6_0 = new() { Value = "net6.0" };

    public static readonly TargetFramework[] NetFramework = {
        NET462
    };

    public static implicit operator string(TargetFramework framework)
    {
        return framework.Value;
    }

    public class TargetFrameworkTypeConverter : TypeConverter<TargetFramework>
    {
        private static readonly TargetFramework[] AllTargetFrameworks = typeof(TargetFramework)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Select(x => x.GetValue(null))
            .Cast<TargetFramework>()
            .ToArray();

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                var matchingFields = AllTargetFrameworks
                    .Where(x => string.Equals(x.Value, stringValue, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                Assert.True(matchingFields.Count == 1);
                return matchingFields.Single();
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}

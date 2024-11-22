using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Nuke.Common.Tooling;
using Serilog;

[TypeConverter(typeof(TargetFrameworkTypeConverter))]
public class TargetFramework : Enumeration
{
    public static readonly TargetFramework NOT_SPECIFIED = new() { Value = string.Empty };
    public static readonly TargetFramework NET462 = new() { Value = "net462" };
    public static readonly TargetFramework NETCore3_1 = new() { Value = "netcoreapp3.1" };
    public static readonly TargetFramework NET8_0 = new() { Value = "net8.0" };
    public static readonly TargetFramework NET9_0 = new() { Value = "net9.0" };

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
            .Where(x => x.FieldType == typeof(TargetFramework))
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
                if (matchingFields.Count == 1)
                {
                    return matchingFields.Single();
                }

                Log.Warning($"Invalid target framework '{stringValue}' falling back to the default value.");
                return NOT_SPECIFIED;
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}

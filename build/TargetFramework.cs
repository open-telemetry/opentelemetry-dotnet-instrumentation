using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Nuke.Common.Tooling;
using Serilog;

[TypeConverter(typeof(TargetFrameworkTypeConverter))]
public class TargetFramework : Enumeration
{
    public static readonly TargetFramework NOT_SPECIFIED = new() { Value = string.Empty, OutputFolder = string.Empty };
    public static readonly TargetFramework NET462 = new() { Value = "net462", OutputFolder = "netfx" };
    public static readonly TargetFramework NET47 = new() { Value = "net47", OutputFolder = "netfx" };
    public static readonly TargetFramework NET471 = new() { Value = "net471", OutputFolder = "netfx" };
    public static readonly TargetFramework NET472 = new() { Value = "net472", OutputFolder = "netfx" };
    public static readonly TargetFramework NET8_0 = new() { Value = "net8.0", OutputFolder = "net" };
    public static readonly TargetFramework NET9_0 = new() { Value = "net9.0", OutputFolder = "net" };
    public static readonly TargetFramework NET10_0 = new() { Value = "net10.0", OutputFolder = "net" };

    public string OutputFolder { get; init; }

    // should be in version order
    public static readonly TargetFramework[] NetFramework = [
        NET462, NET47, NET471, NET472
    ];

    // should be in version order
    public static readonly TargetFramework[] Net = [
        NET8_0, NET9_0, NET10_0
    ];

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

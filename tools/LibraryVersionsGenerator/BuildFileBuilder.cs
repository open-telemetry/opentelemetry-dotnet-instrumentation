// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Text;

namespace LibraryVersionsGenerator;

internal sealed class BuildFileBuilder : CSharpFileBuilder
{
    public override CSharpFileBuilder BeginClass(string classNamespace, string className)
    {
        AddUsing("Models");

        base.BeginClass(classNamespace, className);

        Builder.AppendLine(
            @"    public static IReadOnlyDictionary<string, IReadOnlyCollection<PackageBuildInfo>> Versions = new Dictionary<string, IReadOnlyCollection<PackageBuildInfo>>
    {");

        return this;
    }

    public override CSharpFileBuilder EndClass()
    {
        Builder.AppendLine("    };");

        return base.EndClass();
    }

    public override CSharpFileBuilder BeginTestPackage(string testApplicationName, string integrationName)
    {
        var beginTestPackageTemplate = @$"        {{
            ""{testApplicationName}"",
            [";

        Builder.AppendLine(beginTestPackageTemplate);

        return this;
    }

    public override CSharpFileBuilder AddVersion(string version, string[] supportedFrameworks, string[] supportedPlatforms)
    {
        AddVersion(version, supportedFrameworks, supportedPlatforms, appendEnd: true);

        return this;
    }

    public override CSharpFileBuilder AddVersionWithDependencies(string version, Dictionary<string, string> dependencies, string[] supportedFrameworks, string[] supportedPlatforms)
    {
        AddVersion(version, supportedFrameworks, supportedPlatforms, appendEnd: false);

        Builder.AppendLine(CultureInfo.InvariantCulture, $", additionalMetaData: {SerializeDictionary(dependencies)}),");
        return this;
    }

    public override CSharpFileBuilder EndTestPackage()
    {
        Builder.AppendLine(@"            ]
        },");
        return this;
    }

    private static string SerializeDictionary(Dictionary<string, string> dictionary)
    {
        var dictionarySb = new StringBuilder();
        dictionarySb.Append("new() { ");

        for (var i = 0; i < dictionary.Count; i++)
        {
            var dependency = dictionary.ElementAt(i);

            dictionarySb.Append(CultureInfo.InvariantCulture, $"{{ \"{dependency.Key}\", \"{dependency.Value}\" }}");

            if (i != dictionary.Count - 1)
            {
                dictionarySb.Append(", ");
            }
        }

        dictionarySb.Append(" }");

        return dictionarySb.ToString();
    }

    private static string SerializeArray(string[] array)
    {
        if (array.Length == 0)
        {
            return "[]";
        }

        var arraySb = new StringBuilder();
        arraySb.Append("[ ");

        for (var i = 0; i < array.Length; i++)
        {
            arraySb.Append(CultureInfo.InvariantCulture, $"\"{array[i]}\"");

            if (i != array.Length - 1)
            {
                arraySb.Append(", ");
            }
        }

        arraySb.Append(" ]");

        return arraySb.ToString();
    }

    private BuildFileBuilder AddVersion(string version, string[] supportedFrameworks, string[] supportedPlatforms, bool appendEnd)
    {
        Builder.Append(CultureInfo.InvariantCulture, $"                new(\"{version}\"");

        if (supportedFrameworks.Length > 0)
        {
            Builder.Append(CultureInfo.InvariantCulture, $", supportedFrameworks: {SerializeArray(supportedFrameworks)}");
        }

        if (supportedPlatforms.Length > 0)
        {
            Builder.Append(CultureInfo.InvariantCulture, $", supportedPlatforms: {SerializeArray(supportedPlatforms)}");
        }

        if (appendEnd)
        {
            Builder.AppendLine("),");
        }

        return this;
    }
}

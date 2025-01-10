// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace LibraryVersionsGenerator;

internal sealed class XUnitFileBuilder : CSharpFileBuilder
{
    public override CSharpFileBuilder BeginTestPackage(string testApplicationName, string integrationName)
    {
        Builder.AppendLine(
            @$"    public static TheoryData<string> {integrationName}
    {{
        get
        {{
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else");
        return this;
    }

    public CSharpFileBuilder AddVersion(string version, string[] supportedFrameworks)
    {
        return AddVersion(version, supportedFrameworks, Array.Empty<string>());
    }

    public override CSharpFileBuilder AddVersion(string version, string[] supportedFrameworks, string[] supportedPlatforms)
    {
        var conditionalCompilation = supportedFrameworks.Length > 0;
        if (conditionalCompilation)
        {
            Builder.AppendFormat("#if {0}", string.Join(" || ", supportedFrameworks.Select(x => x.ToUpperInvariant().Replace('.', '_'))));
            Builder.AppendLine();
        }

        Builder.AppendLine($"                \"{version}\",");

        if (conditionalCompilation)
        {
            Builder.AppendLine("#endif");
        }

        return this;
    }

    public override CSharpFileBuilder AddVersionWithDependencies(string version, Dictionary<string, string> dependencies, string[] supportedFrameworks, string[] supportedPlatforms)
    {
        // Dependencies info is currently not usable here. Build is located based on main package version string.
        return AddVersion(version, supportedFrameworks, supportedPlatforms);
    }

    public override CSharpFileBuilder EndTestPackage()
    {
        Builder.AppendLine(@"#endif
            ];
            return theoryData;
        }
    }");

        return this;
    }

    internal void BuildLookupMap(IReadOnlyCollection<PackageVersionDefinitions.PackageVersionDefinition> definitions, List<string> additionalPlatforms)
    {
        Builder.AppendLine(
            @$"    public static readonly IReadOnlyDictionary<string, TheoryData<string>> LookupMap = new Dictionary<string, TheoryData<string>>
    {{");

        foreach (var item in definitions)
        {
            Builder.AppendLine($"       {{ \"{item.IntegrationName}\", {item.IntegrationName} }},");

            if (additionalPlatforms.Any(x => x.StartsWith(item.IntegrationName + "_")))
            {
                foreach (var platform in additionalPlatforms.Where(x => x.StartsWith(item.IntegrationName)))
                {
                    Builder.AppendLine($"       {{ \"{platform}\", {platform} }},");
                }
            }
        }

        Builder.AppendLine("    };");
    }
}

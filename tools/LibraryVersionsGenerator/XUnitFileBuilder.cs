// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace LibraryVersionsGenerator;

internal sealed class XUnitFileBuilder : CSharpFileBuilder
{
    public override CSharpFileBuilder BeginTestPackage(string testApplicationName, string integrationName)
    {
        Builder.AppendLine(
            @$"    public static readonly IReadOnlyCollection<object[]> {integrationName} = new List<object[]>
    {{
#if DEFAULT_TEST_PACKAGE_VERSIONS
        new object[] {{ string.Empty }}
#else");
        return this;
    }

    public override CSharpFileBuilder AddVersion(string version)
    {
        Builder.AppendLine($"        new object[] {{ \"{version}\" }},");
        return this;
    }

    public override CSharpFileBuilder AddVersionWithDependencies(string version, Dictionary<string, string> dependencies)
    {
        // Dependencies info is currently not usable here. Build is located based on main package version string.
        return AddVersion(version);
    }

    public override CSharpFileBuilder EndTestPackage()
    {
        Builder.AppendLine(@"#endif
    };");

        return this;
    }
}

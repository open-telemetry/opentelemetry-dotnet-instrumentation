// <copyright file="XUnitFileBuilder.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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

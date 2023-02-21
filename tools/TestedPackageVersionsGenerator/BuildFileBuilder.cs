// <copyright file="BuildFileBuilder.cs" company="OpenTelemetry Authors">
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

namespace TestedPackageVersionsGenerator;

internal sealed class BuildFileBuilder : CSharpFileBuilder
{
    public override CSharpFileBuilder BeginClass(string classNamespace, string className)
    {
        base.BeginClass(classNamespace, className);

        Builder.AppendLine(
            @"    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> Versions = new Dictionary<string, IReadOnlyCollection<string>>
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
        Builder.AppendLine(
            @$"        {{
            ""{testApplicationName}"",
            new List<string>
            {{");

        return this;
    }

    public override CSharpFileBuilder AddVersion(string version)
    {
        Builder.AppendLine($"                \"{version}\",");
        return this;
    }

    public override CSharpFileBuilder EndTestPackage()
    {
        Builder.AppendLine(@"            }
        },");
        return this;
    }
}

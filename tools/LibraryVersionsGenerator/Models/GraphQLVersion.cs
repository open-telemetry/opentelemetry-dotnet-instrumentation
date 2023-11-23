// <copyright file="GraphQLVersion.cs" company="OpenTelemetry Authors">
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

namespace LibraryVersionsGenerator.Models;

internal class GraphQLVersion : PackageVersion
{
    public GraphQLVersion(string version)
        : base(version)
    {
    }

    [PackageDependency("GraphQL.MicrosoftDI", "GraphQLMicrosoftDI")]
    public required string MicrosoftDIVersion { get; set; }

    [PackageDependency("GraphQL.Server.Transports.AspNetCore", "GraphQLServerTransportsAspNetCore")]
    public required string ServerTransportsAspNetCoreVersion { get; set; }

    [PackageDependency("GraphQL.Server.Ui.Playground", "GraphQLServerUIPlayground")]
    public required string ServerUIPlayground { get; set; }
}

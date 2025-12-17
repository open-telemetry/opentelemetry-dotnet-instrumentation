// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace LibraryVersionsGenerator.Models;

internal sealed class GraphQLVersion : PackageVersion
{
    public GraphQLVersion(string version)
        : base(version)
    {
    }

    [PackageDependency("GraphQL.MicrosoftDI", "GraphQLMicrosoftDI")]
    public required string MicrosoftDIVersion { get; set; }

    [PackageDependency("GraphQL.Server.Transports.AspNetCore", "GraphQLServerTransportsAspNetCore")]
    public required string ServerTransportsAspNetCoreVersion { get; set; }

    [PackageDependency("GraphQL.Server.Ui.GraphiQL", "GraphQLServerUIPGraphiQL")]
    public required string ServerUIGraphiQL { get; set; }
}

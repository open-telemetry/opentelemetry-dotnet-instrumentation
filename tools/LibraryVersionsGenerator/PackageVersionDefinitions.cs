// <copyright file="PackageVersionDefinitions.cs" company="OpenTelemetry Authors">
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

using LibraryVersionsGenerator.Models;

namespace LibraryVersionsGenerator;

internal static class PackageVersionDefinitions
{
    public static IReadOnlyCollection<PackageVersionDefinition> Definitions => new List<PackageVersionDefinition>
    {
        new()
        {
            IntegrationName = "Azure",
            NugetPackageName = "Azure.Storage.Blobs",
            TestApplicationName = "TestApplication.Azure",
            Versions = new List<PackageVersion>
            {
                new("12.13.0"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "Elasticsearch",
            NugetPackageName = "Elastic.Clients.Elasticsearch",
            TestApplicationName = "TestApplication.Elasticsearch",
            Versions = new List<PackageVersion>
            {
                new("8.0.0"),
                new("8.10.0"), // 8.10.0 introduces breaking change for ActivitySource name
                new("*")
            }
        },
        new()
        {
            IntegrationName = "EntityFrameworkCore",
            NugetPackageName = "Microsoft.EntityFrameworkCore.Sqlite",
            TestApplicationName = "TestApplication.EntityFrameworkCore",
            Versions = new List<PackageVersion>
            {
                new("6.0.12"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "EntityFrameworkCorePomeloMySql",
            NugetPackageName = "Pomelo.EntityFrameworkCore.MySql",
            TestApplicationName = "TestApplication.EntityFrameworkCore.Pomelo.MySql",
            Versions = new List<PackageVersion>
            {
                new("6.0.2"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "GraphQL",
            NugetPackageName = "GraphQL",
            TestApplicationName = "TestApplication.GraphQL",
            Versions = new List<GraphQLVersion>
            {
                new("7.5.0") { MicrosoftDIVersion = "7.5.0", ServerTransportsAspNetCoreVersion = "7.5.0", ServerUIPlayground = "7.5.0" },
                new("*") { MicrosoftDIVersion = "*", ServerTransportsAspNetCoreVersion = "*", ServerUIPlayground = "*" },
            }
        },
        new()
        {
            IntegrationName = "GrpcNetClient",
            NugetPackageName = "Grpc.Net.Client",
            TestApplicationName = "TestApplication.GrpcNetClient",
            Versions = new List<PackageVersion>
            {
                new("2.52.0"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "MassTransit",
            NugetPackageName = "MassTransit",
            TestApplicationName = "TestApplication.MassTransit",
            Versions = new List<PackageVersion>
            {
                new("8.0.0"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "SqlClient",
            NugetPackageName = "Microsoft.Data.SqlClient",
            TestApplicationName = "TestApplication.SqlClient",
            Versions = new List<PackageVersion>
            {
                new("1.1.4"),
                new("2.1.5"),
                new("3.1.2"),
                new("4.1.1"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "MongoDB",
            NugetPackageName = "MongoDB.Driver",
            TestApplicationName = "TestApplication.MongoDB",
            Versions = new List<PackageVersion>
            {
                // new("2.13.3"), - high vulnarability https://github.com/advisories/GHSA-7j9m-j397-g4wx, <= 2.18.0 test should be skipped
                // new("2.15.0"), - high vulnarability https://github.com/advisories/GHSA-7j9m-j397-g4wx, <= 2.18.0 test should be skipped
                new("2.19.0"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "MySqlConnector",
            NugetPackageName = "MySqlConnector",
            TestApplicationName = "TestApplication.MySqlConnector",
            Versions = new List<PackageVersion>
            {
                new("2.0.0"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "MySqlData",
            NugetPackageName = "MySql.Data",
            TestApplicationName = "TestApplication.MySqlData",
            Versions = new List<PackageVersion>
            {
                new("8.1.0"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "Npgsql",
            NugetPackageName = "Npgsql",
            TestApplicationName = "TestApplication.Npgsql",
            Versions = new List<PackageVersion>
            {
                new("6.0.0"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "NServiceBus",
            NugetPackageName = "NServiceBus",
            TestApplicationName = "TestApplication.NServiceBus",
            Versions = new List<PackageVersion>
            {
                new("8.0.0"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "Quartz",
            NugetPackageName = "Quartz",
            TestApplicationName = "TestApplication.Quartz",
            Versions = new List<PackageVersion>
            {
                new("3.4.0"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "StackExchangeRedis",
            NugetPackageName = "StackExchange.Redis",
            TestApplicationName = "TestApplication.StackExchangeRedis",
            Versions = new List<PackageVersion>
            {
                new("2.0.495"),
                new("2.1.50"),
                new("2.5.61"),
                new("2.6.66"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "WCFCoreClient",
            NugetPackageName = "System.ServiceModel.Http",
            TestApplicationName = "TestApplication.Wcf.Client.DotNet",
            Versions = new List<PackageVersion>
            {
                new("4.10.2"),
                new("*")
            }
        }
    };

    internal record PackageVersionDefinition
    {
        required public string IntegrationName { get; init; }

        required public string NugetPackageName { get; init; }

        required public string TestApplicationName { get; init; }

        required public IReadOnlyCollection<PackageVersion> Versions { get; init; }
    }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
                new("6.0.27"),
                new("7.0.16"),
                new("8.0.2", "net8.0"),
                new("*", "net8.0")
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
                new("7.0.0"),
                new("8.0.0", "net8.0"),
                new("*", "net8.0")
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
            IntegrationName = "SqlClientMicrosoft",
            NugetPackageName = "Microsoft.Data.SqlClient",
            TestApplicationName = "TestApplication.SqlClient.Microsoft",
            Versions = new List<PackageVersion>
            {
                // new("1.1.4"), - high vulnerability https://github.com/dotnet/announcements/issues/292, test should be skipped
                new("2.1.7"),
                new("3.1.5"),
                new("4.0.5"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "SqlClientSystem",
            NugetPackageName = "System.Data.SqlClient",
            TestApplicationName = "TestApplication.SqlClient.System",
            Versions = new List<PackageVersion>
            {
                new("4.8.6"),
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
                // new("2.13.3"), - high vulnerability https://github.com/advisories/GHSA-7j9m-j397-g4wx, <= 2.18.0 test should be skipped
                // new("2.15.0"), - high vulnerability https://github.com/advisories/GHSA-7j9m-j397-g4wx, <= 2.18.0 test should be skipped
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
        },
        new()
        {
            IntegrationName = "Kafka",
            NugetPackageName = "Confluent.Kafka",
            TestApplicationName = "TestApplication.Kafka",
            Versions = new List<PackageVersion>
            {
                // TODO: Temporarily disable version 1.4.0, Doesn't support ARM64
                // new("1.4.0"),
                new("*")
            }
        }
    };

    internal record PackageVersionDefinition
    {
        public required string IntegrationName { get; init; }

        public required string NugetPackageName { get; init; }

        public required string TestApplicationName { get; init; }

        public required IReadOnlyCollection<PackageVersion> Versions { get; init; }
    }
}

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
                // new("12.13.0"), // all lower versions than 12.22.2 contains references impacted by https://github.com/advisories/GHSA-8g4q-xg66-9fp4
                new("12.22.2"),
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
                /*
                new("8.0.0"),
                new("8.10.0"),
                all lower versions than 8.15.10 contains references impacted by
                https://github.com/advisories/GHSA-8g4q-xg66-9fp4
                */
                new("8.15.10"),
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
                new("6.0.35"),
                // new("7.0.20"), all versions contains references to vulnerable packages https://github.com/advisories/GHSA-hh2w-p6rv-4g7w
                new("8.0.10"),
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
                new("6.0.3"),
                new("7.0.0"),
                new("8.0.0"),
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
                new("7.5.0") { MicrosoftDIVersion = "7.5.0", ServerTransportsAspNetCoreVersion = "7.5.0", ServerUIGraphiQL = "7.5.0" },
                new("8.0.2") { MicrosoftDIVersion = "8.0.2", ServerTransportsAspNetCoreVersion = "8.0.2", ServerUIGraphiQL = "8.0.2" },
                new("*") { MicrosoftDIVersion = "*", ServerTransportsAspNetCoreVersion = "*", ServerUIGraphiQL = "*" },
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
            IntegrationName = "log4net",
            NugetPackageName = "log4net",
            TestApplicationName = "TestApplication.Log4NetBridge",
            Versions = new List<PackageVersion>
            {
                // versions below 2.0.10 have critical vulnerabilities
                // versions below 2.0.13 have known bugs e.g. https://issues.apache.org/jira/browse/LOG4NET-652
                new("2.0.13"),
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
                // new("8.0.0"), // all lower versions than 8.3.0 contains references impacted by
                // https://github.com/advisories/GHSA-8g4q-xg66-9fp4
                new("8.3.0"),
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
                // new("2.1.7"), transitive vulnerabilities https://github.com/advisories/GHSA-rxg9-xrhp-64gj
                // new("3.1.7", supportedTargetFrameworks: new[] { "net8.0" }, supportedExecutionFrameworks: new[] { "net8.0" }), // 3.1.* is not supported on .NET Framework. For details check: https://github.com/open-telemetry/opentelemetry-dotnet/issues/4243, transitive vulnerabilities https://github.com/advisories/GHSA-rxg9-xrhp-64gj
                // new("4.0.6"), transitive vulnerabilities https://github.com/advisories/GHSA-rxg9-xrhp-64gj
                new("5.2.2"),
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
                // new("2.7.0"), - high vulnerability https://github.com/advisories/GHSA-7j9m-j397-g4wx, < 2.19.0
                new("2.19.0", supportedTargetFrameworks: ["net10.0", "net9.0", "net8.0", "net462"], supportedExecutionFrameworks: ["net10.0", "net9.0", "net8.0", "net462"]),
                new("2.30.0", supportedTargetFrameworks: ["net10.0", "net9.0", "net8.0", "net462"], supportedExecutionFrameworks: ["net10.0", "net9.0", "net8.0", "net462"]),
                new("3.0.0", supportedTargetFrameworks: ["net10.0", "net9.0", "net8.0", "net472"], supportedExecutionFrameworks: ["net10.0", "net9.0", "net8.0", "net462"]),
                new("*", supportedTargetFrameworks: ["net10.0", "net9.0", "net8.0", "net472"], supportedExecutionFrameworks: ["net10.0", "net9.0", "net8.0", "net462"])
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
                // new("8.1.0"), transitive vulnerability, https://github.com/advisories/GHSA-rxg9-xrhp-64gj, <9.0.0
                new("9.0.0"),
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
                // new("6.0.0"), - high vulnerability https://github.com/advisories/GHSA-x9vc-6hfv-hg8c, <= 6.0.10, <= 7.0.6, and <= 8.0.2 test should be skipped
                // new("6.0.11"), - transitive vulnerabilities https://github.com/advisories/GHSA-8g4q-xg66-9fp4 <= 6.0.12, <=7.0.8, <=8.0.4 test should be skipped
                new("8.0.5"),
                new("*", supportedTargetFrameworks: ["net10.0", "net9.0", "net8.0"], supportedExecutionFrameworks: ["net10.0", "net9.0", "net8.0"])
            }
        },
        new()
        {
            IntegrationName = "NServiceBus",
            NugetPackageName = "NServiceBus",
            TestApplicationName = "TestApplication.NServiceBus",
            Versions = new List<PackageVersion>
            {
                // new("8.0.0"), - transitive vulnerabilities https://github.com/advisories/GHSA-8g4q-xg66-9fp4, <=8.2.3
                new("8.2.5"),
                new("9.1.0", supportedTargetFrameworks: ["net10.0", "net9.0", "net8.0"], supportedExecutionFrameworks: ["net10.0", "net9.0", "net8.0"]), // breaking change, new Meter name
                new("*", supportedTargetFrameworks: ["net10.0", "net9.0", "net8.0"], supportedExecutionFrameworks: ["net10.0", "net9.0", "net8.0"])
            }
        },
        new()
        {
            IntegrationName = "OracleMda",
            NugetPackageName = "Oracle.ManagedDataAccess",
            TestApplicationName = "TestApplication.OracleMda.NetFramework",
            Versions = new List<PackageVersion>
            {
                // new("23.4.0", supportedTargetFrameworks: new[] { "net472" }, supportedExecutionFrameworks: new[] { "net462" }), transitive vulnerability https://github.com/advisories/GHSA-447r-wph3-92pm, <= 23.5.0
                new("23.5.1", supportedTargetFrameworks: ["net472"], supportedExecutionFrameworks: ["net462"]),
                new("*", supportedTargetFrameworks: ["net472"], supportedExecutionFrameworks: ["net462"])
            }
        },
        new()
        {
            IntegrationName = "OracleMdaCore",
            NugetPackageName = "Oracle.ManagedDataAccess.Core",
            TestApplicationName = "TestApplication.OracleMda.Core",
            Versions = new List<PackageVersion>
            {
                // new("23.4.0"), transitive vulnerability https://github.com/advisories/GHSA-447r-wph3-92pm, <= 23.5.0
                new("23.5.1"),
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
                // new("3.4.0"), - transitive vulnerability https://github.com/advisories/GHSA-rxg9-xrhp-64gj, <= 3.5.0
                new("3.6.0"),
                new("*")
            }
        },
        new()
        {
            IntegrationName = "RabbitMq",
            NugetPackageName = "RabbitMQ.Client",
            TestApplicationName = "TestApplication.RabbitMq",
            Versions = new List<PackageVersion>
            {
                new("5.1.2"),
                new("6.8.1"),
                new("7.0.0"),
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
                new("2.6.122"),
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
                new("6.2.0"),
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
                // new("1.4.0", supportedPlatforms: ["x64"]), 1.8.0 and lower versions have transitive vulnerabilities https://github.com/confluentinc/confluent-kafka-dotnet/blob/fa0f92a4593e5a19b5a052b633ddf47fee47588c/CHANGELOG.md#security
                // new("1.6.2"), // First version that supports both arm64 and x64, 1.8.0 and lower versions have transitive vulnerabilities https://github.com/confluentinc/confluent-kafka-dotnet/blob/fa0f92a4593e5a19b5a052b633ddf47fee47588c/CHANGELOG.md#security
                new("1.8.2"), // 1.8.0-1.8.1 are known to have issues with arm64, 1.8.0 and lower versions have transitive vulnerabilities https://github.com/confluentinc/confluent-kafka-dotnet/blob/fa0f92a4593e5a19b5a052b633ddf47fee47588c/CHANGELOG.md#security
                new("1.9.2"), // First version supported on macOS ARM64
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

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

namespace LibraryVersionsGenerator;

internal static class PackageVersionDefinitions
{
    public static IReadOnlyCollection<PackageVersionDefinition> Definitions => new List<PackageVersionDefinition>
    {
        new()
        {
            IntegrationName = "Elasticsearch",
            NugetPackageName = "Elastic.Clients.Elasticsearch",
            TestApplicationName = "TestApplication.Elasticsearch",
            Versions = new List<string>
            {
                "8.0.0",
                "*"
            }
        },
        new()
        {
            IntegrationName = "EntityFrameworkCore",
            NugetPackageName = "Microsoft.EntityFrameworkCore.Sqlite",
            TestApplicationName = "TestApplication.EntityFrameworkCore",
            Versions = new List<string>
            {
                "6.0.12",
                "*"
            }
        },
        new()
        {
            IntegrationName = "EntityFrameworkCorePomeloMySql",
            NugetPackageName = "Pomelo.EntityFrameworkCore.MySql",
            TestApplicationName = "TestApplication.EntityFrameworkCore.Pomelo.MySql",
            Versions = new List<string>
            {
                "6.0.2",
                "*"
            }
        },
        new()
        {
            IntegrationName = "GraphQL",
            NugetPackageName = "GraphQL",
            TestApplicationName = "TestApplication.GraphQL",
            Versions = new List<string>
            {
                "2.3.0",
                "*"
            }
        },
        new()
        {
            IntegrationName = "GrpcNetClient",
            NugetPackageName = "Grpc.Net.Client",
            TestApplicationName = "TestApplication.GrpcNetClient",
            Versions = new List<string>
            {
                "2.43.0",
                "*"
            }
        },
        new()
        {
            IntegrationName = "MassTransit",
            NugetPackageName = "MassTransit",
            TestApplicationName = "TestApplication.MassTransit",
            Versions = new List<string>
            {
                "8.0.0",
                "*"
            }
        },
        new()
        {
            IntegrationName = "SqlClient",
            NugetPackageName = "Microsoft.Data.SqlClient",
            TestApplicationName = "TestApplication.SqlClient",
            Versions = new List<string>
            {
                "1.1.4",
                "2.1.5",
                "3.1.2",
                "4.1.1",
                "*"
            }
        },
        new()
        {
            IntegrationName = "MongoDB",
            NugetPackageName = "MongoDB.Driver",
            TestApplicationName = "TestApplication.MongoDB",
            Versions = new List<string>
            {
                "2.13.3",
                "2.15.0",
                "*"
            }
        },
        new()
        {
            IntegrationName = "MySqlData",
            NugetPackageName = "MySql.Data",
            TestApplicationName = "TestApplication.MySqlData",
            Versions = new List<string>
            {
                "6.10.7",
                "*"
            }
        },
        new()
        {
            IntegrationName = "Npgsql",
            NugetPackageName = "Npgsql",
            TestApplicationName = "TestApplication.Npgsql",
            Versions = new List<string>
            {
                "6.0.0",
                "*"
            }
        },
        new()
        {
            IntegrationName = "NServiceBus",
            NugetPackageName = "NServiceBus",
            TestApplicationName = "TestApplication.NServiceBus",
            Versions = new List<string>
            {
                "8.0.0",
                "*"
            }
        },
        new()
        {
            IntegrationName = "Quartz",
            NugetPackageName = "Quartz",
            TestApplicationName = "TestApplication.Quartz",
            Versions = new List<string>
            {
                "3.4.0",
                "*"
            }
        },
        new()
        {
            IntegrationName = "StackExchangeRedis",
            NugetPackageName = "StackExchange.Redis",
            TestApplicationName = "TestApplication.StackExchangeRedis",
            Versions = new List<string>
            {
                "2.0.495",
                "2.1.50",
                "2.5.61",
                "2.6.66",
                "*"
            }
        }
    };

    internal record PackageVersionDefinition
    {
        required public string IntegrationName { get; init; }

        required public string NugetPackageName { get; init; }

        required public string TestApplicationName { get; init; }

        required public IReadOnlyCollection<string> Versions { get; init; }
    }
}

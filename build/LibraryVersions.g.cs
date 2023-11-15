//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the LibraryVersionsGenerator tool. To safely
//     modify this file, edit PackageVersionDefinitions.cs and
//     re-run the LibraryVersionsGenerator project in Visual Studio.
// 
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated. 
// </auto-generated>
//------------------------------------------------------------------------------

using Models;

public static class LibraryVersion
{
    public static IReadOnlyDictionary<string, IReadOnlyCollection<PackageBuildInfo>> Versions = new Dictionary<string, IReadOnlyCollection<PackageBuildInfo>>
    {
        {
            "TestApplication.Azure",
            new List<PackageBuildInfo>
            {
                new("12.13.0"),
                new("12.19.1"),
            }
        },
        {
            "TestApplication.Elasticsearch",
            new List<PackageBuildInfo>
            {
                new("8.0.0"),
                new("8.10.0"),
            }
        },
        {
            "TestApplication.EntityFrameworkCore",
            new List<PackageBuildInfo>
            {
                new("6.0.12"),
                new("7.0.13"),
            }
        },
        {
            "TestApplication.EntityFrameworkCore.Pomelo.MySql",
            new List<PackageBuildInfo>
            {
                new("6.0.2"),
                new("7.0.0"),
            }
        },
        {
            "TestApplication.GraphQL",
            new List<PackageBuildInfo>
            {
                new("7.5.0", new() {{"GraphQLMicrosoftDI","7.5.0"},{"GraphQLServerTransportsAspNetCore","7.5.0"},{"GraphQLServerUIPlayground","7.5.0"}}),
                new("7.6.1", new() {{"GraphQLMicrosoftDI","7.6.1"},{"GraphQLServerTransportsAspNetCore","7.6.0"},{"GraphQLServerUIPlayground","7.6.0"}}),
            }
        },
        {
            "TestApplication.GrpcNetClient",
            new List<PackageBuildInfo>
            {
                new("2.52.0"),
                new("2.59.0"),
            }
        },
        {
            "TestApplication.MassTransit",
            new List<PackageBuildInfo>
            {
                new("8.0.0"),
                new("8.1.1"),
            }
        },
        {
            "TestApplication.SqlClient.Microsoft",
            new List<PackageBuildInfo>
            {
                new("1.1.4"),
                new("2.1.5"),
                new("3.1.2"),
                new("4.1.1"),
                new("5.1.2"),
            }
        },
        {
            "TestApplication.SqlClient.System",
            new List<PackageBuildInfo>
            {
                new("4.8.5"),
            }
        },
        {
            "TestApplication.MongoDB",
            new List<PackageBuildInfo>
            {
                new("2.13.3"),
                new("2.15.0"),
                new("2.22.0"),
            }
        },
        {
            "TestApplication.MySqlConnector",
            new List<PackageBuildInfo>
            {
                new("2.0.0"),
                new("2.3.0"),
            }
        },
        {
            "TestApplication.MySqlData",
            new List<PackageBuildInfo>
            {
                new("8.1.0"),
                new("8.2.0"),
            }
        },
        {
            "TestApplication.Npgsql",
            new List<PackageBuildInfo>
            {
                new("6.0.0"),
                new("7.0.6"),
            }
        },
        {
            "TestApplication.NServiceBus",
            new List<PackageBuildInfo>
            {
                new("8.0.0"),
                new("8.1.6"),
            }
        },
        {
            "TestApplication.Quartz",
            new List<PackageBuildInfo>
            {
                new("3.4.0"),
                new("3.7.0"),
            }
        },
        {
            "TestApplication.StackExchangeRedis",
            new List<PackageBuildInfo>
            {
                new("2.0.495"),
                new("2.1.50"),
                new("2.5.61"),
                new("2.6.66"),
                new("2.7.4"),
            }
        },
        {
            "TestApplication.Wcf.Client.DotNet",
            new List<PackageBuildInfo>
            {
                new("4.10.2"),
                new("6.2.0"),
            }
        },
    };
}

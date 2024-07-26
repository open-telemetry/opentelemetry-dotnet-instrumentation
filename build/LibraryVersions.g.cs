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

public static partial class LibraryVersion
{
    public static IReadOnlyDictionary<string, IReadOnlyCollection<PackageBuildInfo>> Versions = new Dictionary<string, IReadOnlyCollection<PackageBuildInfo>>
    {
        {
            "TestApplication.Azure",
            new List<PackageBuildInfo>
            {
                new("12.13.0"),
                new("12.20.0"),
            }
        },
        {
            "TestApplication.Elasticsearch",
            new List<PackageBuildInfo>
            {
                new("8.0.0"),
                new("8.10.0"),
                new("8.14.4"),
            }
        },
        {
            "TestApplication.EntityFrameworkCore",
            new List<PackageBuildInfo>
            {
                new("6.0.27"),
                new("7.0.20"),
                new("8.0.2", supportedFrameworks: new string[] {"net8.0"}),
                new("8.0.6", supportedFrameworks: new string[] {"net8.0"}),
            }
        },
        {
            "TestApplication.EntityFrameworkCore.Pomelo.MySql",
            new List<PackageBuildInfo>
            {
                new("6.0.2"),
                new("7.0.0"),
                new("8.0.0", supportedFrameworks: new string[] {"net8.0"}),
                new("8.0.2", supportedFrameworks: new string[] {"net8.0"}),
            }
        },
        {
            "TestApplication.GraphQL",
            new List<PackageBuildInfo>
            {
                new("7.5.0", additionalMetaData: new() {{"GraphQLMicrosoftDI","7.5.0"},{"GraphQLServerTransportsAspNetCore","7.5.0"},{"GraphQLServerUIPlayground","7.5.0"}}),
                new("7.8.0", additionalMetaData: new() {{"GraphQLMicrosoftDI","7.8.0"},{"GraphQLServerTransportsAspNetCore","7.7.1"},{"GraphQLServerUIPlayground","7.7.1"}}),
            }
        },
        {
            "TestApplication.GrpcNetClient",
            new List<PackageBuildInfo>
            {
                new("2.52.0"),
                new("2.64.0"),
            }
        },
        {
            "TestApplication.MassTransit",
            new List<PackageBuildInfo>
            {
                new("8.0.0"),
                new("8.2.3"),
            }
        },
        {
            "TestApplication.SqlClient.Microsoft",
            new List<PackageBuildInfo>
            {
                new("2.1.7"),
                new("3.1.5"),
                new("4.0.5"),
                new("5.2.1"),
            }
        },
        {
            "TestApplication.SqlClient.System",
            new List<PackageBuildInfo>
            {
                new("4.8.6"),
            }
        },
        {
            "TestApplication.MongoDB",
            new List<PackageBuildInfo>
            {
                new("2.19.0"),
                new("2.27.0"),
            }
        },
        {
            "TestApplication.MySqlConnector",
            new List<PackageBuildInfo>
            {
                new("2.0.0"),
                new("2.3.7"),
            }
        },
        {
            "TestApplication.MySqlData",
            new List<PackageBuildInfo>
            {
                new("8.1.0"),
                new("9.0.0"),
            }
        },
        {
            "TestApplication.Npgsql",
            new List<PackageBuildInfo>
            {
                new("6.0.11"),
                new("8.0.3"),
            }
        },
        {
            "TestApplication.NServiceBus",
            new List<PackageBuildInfo>
            {
                new("8.0.0"),
                new("9.0.2", supportedFrameworks: new string[] {"net8.0"}),
            }
        },
        {
            "TestApplication.OracleMda.NetFramework",
            new List<PackageBuildInfo>
            {
                new("23.4.0", supportedFrameworks: new string[] {"net472"}),
            }
        },
        {
            "TestApplication.OracleMda.Core",
            new List<PackageBuildInfo>
            {
                new("23.4.0"),
            }
        },
        {
            "TestApplication.Quartz",
            new List<PackageBuildInfo>
            {
                new("3.4.0"),
                new("3.10.0"),
            }
        },
        {
            "TestApplication.StackExchangeRedis",
            new List<PackageBuildInfo>
            {
                new("2.6.122"),
                new("2.8.0"),
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
        {
            "TestApplication.Kafka",
            new List<PackageBuildInfo>
            {
                new("1.4.0", supportedPlatforms: new string[] {"x64"}),
                new("1.6.2"),
                new("1.8.2"),
                new("2.4.0"),
            }
        },
        {
            "TestApplication.RabbitMq",
            new List<PackageBuildInfo>
            {
                new("6.0.0"),
                new("6.8.1"),
            }
        },
    };
}

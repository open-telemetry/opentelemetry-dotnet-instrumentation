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
            [
                new("12.22.2"),
                new("12.23.0"),
            ]
        },
        {
            "TestApplication.Elasticsearch",
            [
                new("8.15.10"),
                new("8.17.1"),
            ]
        },
        {
            "TestApplication.EntityFrameworkCore",
            [
                new("6.0.35"),
                new("8.0.10"),
                new("9.0.1"),
            ]
        },
        {
            "TestApplication.EntityFrameworkCore.Pomelo.MySql",
            [
                new("6.0.3"),
                new("7.0.0"),
                new("8.0.0"),
                new("8.0.2"),
            ]
        },
        {
            "TestApplication.GraphQL",
            [
                new("7.5.0", additionalMetaData: new() { { "GraphQLMicrosoftDI", "7.5.0" }, { "GraphQLServerTransportsAspNetCore", "7.5.0" }, { "GraphQLServerUIPGraphiQL", "7.5.0" } }),
                new("8.0.2", additionalMetaData: new() { { "GraphQLMicrosoftDI", "8.0.2" }, { "GraphQLServerTransportsAspNetCore", "8.0.2" }, { "GraphQLServerUIPGraphiQL", "8.0.2" } }),
                new("8.3.0", additionalMetaData: new() { { "GraphQLMicrosoftDI", "8.3.0" }, { "GraphQLServerTransportsAspNetCore", "8.2.0" }, { "GraphQLServerUIPGraphiQL", "8.2.0" } }),
            ]
        },
        {
            "TestApplication.GrpcNetClient",
            [
                new("2.52.0"),
                new("2.67.0"),
            ]
        },
        {
            "TestApplication.Log4NetBridge",
            [
                new("2.0.13"),
                new("3.0.3"),
            ]
        },
        {
            "TestApplication.MassTransit",
            [
                new("8.3.0"),
                new("8.3.4"),
            ]
        },
        {
            "TestApplication.SqlClient.Microsoft",
            [
                new("5.2.2"),
                new("6.0.1"),
            ]
        },
        {
            "TestApplication.SqlClient.System",
            [
                new("4.8.6"),
                new("4.9.0"),
            ]
        },
        {
            "TestApplication.MongoDB",
            [
                new("2.19.0", supportedFrameworks: [ "net9.0", "net8.0", "net462" ]),
                new("2.30.0", supportedFrameworks: [ "net9.0", "net8.0", "net462" ]),
                new("3.0.0", supportedFrameworks: [ "net9.0", "net8.0", "net472" ]),
                new("3.1.0", supportedFrameworks: [ "net9.0", "net8.0", "net472" ]),
            ]
        },
        {
            "TestApplication.MySqlConnector",
            [
                new("2.0.0"),
                new("2.4.0"),
            ]
        },
        {
            "TestApplication.MySqlData",
            [
                new("9.0.0"),
                new("9.1.0"),
            ]
        },
        {
            "TestApplication.Npgsql",
            [
                new("8.0.5"),
                new("9.0.2", supportedFrameworks: [ "net9.0", "net8.0" ]),
            ]
        },
        {
            "TestApplication.NServiceBus",
            [
                new("8.2.5"),
                new("9.1.0", supportedFrameworks: [ "net9.0", "net8.0" ]),
                new("9.2.6", supportedFrameworks: [ "net9.0", "net8.0" ]),
            ]
        },
        {
            "TestApplication.OracleMda.NetFramework",
            [
                new("23.5.1", supportedFrameworks: [ "net472" ]),
                new("23.6.1", supportedFrameworks: [ "net472" ]),
            ]
        },
        {
            "TestApplication.OracleMda.Core",
            [
                new("23.5.1"),
                new("23.6.1"),
            ]
        },
        {
            "TestApplication.Quartz",
            [
                new("3.6.0"),
                new("3.13.1"),
            ]
        },
        {
            "TestApplication.RabbitMq",
            [
                new("6.0.0"),
                new("6.8.1"),
                new("7.0.0"),
            ]
        },
        {
            "TestApplication.StackExchangeRedis",
            [
                new("2.6.122"),
                new("2.8.22"),
            ]
        },
        {
            "TestApplication.Wcf.Client.DotNet",
            [
                new("4.10.2"),
                new("6.2.0"),
                new("8.1.0"),
            ]
        },
        {
            "TestApplication.Kafka",
            [
                new("1.4.0", supportedPlatforms: [ "x64" ]),
                new("1.6.2"),
                new("1.8.2"),
                new("2.8.0"),
            ]
        },
    };
}

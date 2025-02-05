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

namespace IntegrationTests;

public static partial class LibraryVersion
{
    public static TheoryData<string> Azure
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "12.22.2",
                "12.23.0",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> Elasticsearch
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "8.15.10",
                "8.17.1",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> EntityFrameworkCore
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "6.0.35",
                "8.0.10",
                "9.0.1",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> EntityFrameworkCorePomeloMySql
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "6.0.3",
                "7.0.0",
                "8.0.0",
                "8.0.2",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> GraphQL
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "7.5.0",
                "8.0.2",
                "8.3.0",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> GrpcNetClient
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "2.52.0",
                "2.67.0",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> log4net
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "2.0.13",
                "3.0.3",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> MassTransit
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "8.3.0",
                "8.3.4",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> SqlClientMicrosoft
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "5.2.2",
                "6.0.1",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> SqlClientSystem
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "4.8.6",
                "4.9.0",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> MongoDB
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
#if NET9_0 || NET8_0 || NET462
                "2.19.0",
#endif
#if NET9_0 || NET8_0 || NET462
                "2.30.0",
#endif
#if NET9_0 || NET8_0 || NET462
                "3.0.0",
#endif
#if NET9_0 || NET8_0 || NET462
                "3.1.0",
#endif
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> MySqlConnector
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "2.0.0",
                "2.4.0",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> MySqlData
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "9.0.0",
                "9.1.0",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> Npgsql
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "8.0.5",
#if NET9_0 || NET8_0
                "9.0.2",
#endif
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> NServiceBus
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "8.2.4",
#if NET9_0 || NET8_0
                "9.1.0",
#endif
#if NET9_0 || NET8_0
                "9.2.3",
#endif
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> OracleMda
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
#if NET462
                "23.5.1",
#endif
#if NET462
                "23.6.1",
#endif
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> OracleMdaCore
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "23.5.1",
                "23.6.1",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> Quartz
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "3.6.0",
                "3.13.1",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> RabbitMq
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "6.0.0",
                "6.8.1",
                "7.0.0",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> StackExchangeRedis
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "2.6.122",
                "2.8.22",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> WCFCoreClient
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "4.10.2",
                "6.2.0",
                "8.1.0",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> Kafka
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "1.6.2",
                "1.8.2",
                "2.8.0",
#endif
            ];
            return theoryData;
        }
    }
    public static TheoryData<string> Kafka_x64
    {
        get
        {
            TheoryData<string> theoryData =
            [
#if DEFAULT_TEST_PACKAGE_VERSIONS
                string.Empty,
#else
                "1.4.0",
#endif
            ];
            return theoryData;
        }
    }
    public static readonly IReadOnlyDictionary<string, TheoryData<string>> LookupMap = new Dictionary<string, TheoryData<string>>
    {
       { "Azure", Azure },
       { "Elasticsearch", Elasticsearch },
       { "EntityFrameworkCore", EntityFrameworkCore },
       { "EntityFrameworkCorePomeloMySql", EntityFrameworkCorePomeloMySql },
       { "GraphQL", GraphQL },
       { "GrpcNetClient", GrpcNetClient },
       { "log4net", log4net },
       { "MassTransit", MassTransit },
       { "SqlClientMicrosoft", SqlClientMicrosoft },
       { "SqlClientSystem", SqlClientSystem },
       { "MongoDB", MongoDB },
       { "MySqlConnector", MySqlConnector },
       { "MySqlData", MySqlData },
       { "Npgsql", Npgsql },
       { "NServiceBus", NServiceBus },
       { "OracleMda", OracleMda },
       { "OracleMdaCore", OracleMdaCore },
       { "Quartz", Quartz },
       { "RabbitMq", RabbitMq },
       { "StackExchangeRedis", StackExchangeRedis },
       { "WCFCoreClient", WCFCoreClient },
       { "Kafka", Kafka },
       { "Kafka_x64", Kafka_x64 },
    };
}

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
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("12.22.2");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> Elasticsearch
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("8.15.10");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> EntityFrameworkCore
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("6.0.35");
            theoryData.Add("8.0.10");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> EntityFrameworkCorePomeloMySql
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("6.0.3");
            theoryData.Add("7.0.0");
            theoryData.Add("8.0.0");
            theoryData.Add("8.0.2");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> GraphQL
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("7.5.0");
            theoryData.Add("8.0.2");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> GrpcNetClient
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("2.52.0");
            theoryData.Add("2.66.0");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> MassTransit
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("8.3.0");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> SqlClientMicrosoft
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("5.2.2");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> SqlClientSystem
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("4.8.6");
            theoryData.Add("4.9.0");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> MongoDB
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("2.19.0");
            theoryData.Add("2.30.0");
#if NET9_0 || NET8_0
            theoryData.Add("3.0.0");
#endif
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> MySqlConnector
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("2.0.0");
            theoryData.Add("2.3.7");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> MySqlData
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("9.0.0");
            theoryData.Add("9.1.0");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> Npgsql
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("8.0.5");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> NServiceBus
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("8.2.4");
#if NET9_0 || NET8_0
            theoryData.Add("9.1.0");
#endif
#if NET9_0 || NET8_0
            theoryData.Add("9.2.3");
#endif
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> OracleMda
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
#if NET462
            theoryData.Add("23.5.1");
#endif
#if NET462
            theoryData.Add("23.6.0");
#endif
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> OracleMdaCore
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("23.5.1");
            theoryData.Add("23.6.0");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> Quartz
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("3.6.0");
            theoryData.Add("3.13.0");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> RabbitMq
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("6.0.0");
            theoryData.Add("6.8.1");
            theoryData.Add("7.0.0");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> StackExchangeRedis
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("2.6.122");
            theoryData.Add("2.8.12");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> WCFCoreClient
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("4.10.2");
            theoryData.Add("6.2.0");
#if NET9_0 || NET8_0
            theoryData.Add("8.0.0");
#endif
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> Kafka
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("1.6.2");
            theoryData.Add("1.8.2");
            theoryData.Add("2.5.3");
#endif
            return theoryData;
        }
    }
    public static TheoryData<string> Kafka_x64
    {
        get
        {
            var theoryData = new TheoryData<string>();
#if DEFAULT_TEST_PACKAGE_VERSIONS
            theoryData.Add(string.Empty);
#else
            theoryData.Add("1.4.0");
#endif
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

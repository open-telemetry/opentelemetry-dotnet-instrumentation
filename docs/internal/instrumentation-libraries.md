# Support for OpenTelemetry Instrumentation Libraries

OpenTelemetry instrumentation is implemented using a variety of techniques.
Sometimes the instrumentation is built directly into the
[instrumented library](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/glossary.md#instrumented-library),
sometimes the instrumentation requires the addition of a separate
[instrumentation library](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/glossary.md#instrumentation-library),
and other times it is a hybrid of those approaches. This
project aims to make using these instrumentation approaches easier and more
automatic for our users. This requires us to both vet and dynamically enable a
subset of the instrumentation that is available to the OpenTelemetry community,
while still providing some flexibility to allow users to manually load and enable
instrumentation that we do not support out of the box.

## Libraries and our instrumentation support plans

The libraries listed here contain our current thoughts about which libraries should
include out of the box support, and which libraries we decided to not support. If
you have a library that you would like to consider adding to this list, please
submit an issue to request its inclusion.

### Application Frameworks

| Framework | [Tracing Support](../config.md#instrumented-traces-libraries-and-frameworks) | [Metrics Support](../config.md#instrumented-metrics-libraries-and-frameworks) | Notes |
| --- | :---: | :---: | --- |
| [ASP.NET](https://docs.microsoft.com/en-us/aspnet/overview) | Yes | Yes | |
| [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/introduction-to-aspnet-core?view=aspnetcore-6.0) | Yes | Yes | |
| [Windows Workflow Foundation](https://docs.microsoft.com/en-us/dotnet/framework/windows-workflow-foundation/) | | | This is .NET Framework only. There is an [experimental port for .NET 6](https://github.com/UiPath/CoreWF). | |
| [OWIN](https://docs.microsoft.com/en-us/aspnet/aspnet/overview/owin-and-katana/) | | | This is .NET Framework only. |

### Databases

| Library | [Tracing Support](../config.md#instrumented-traces-libraries-and-frameworks) | [Metrics Support](../config.md#instrumented-metrics-libraries-and-frameworks)| Databases Tested | Notes |
| --- | :---: | :---: | --- | --- |
| [Entity Framework](https://docs.microsoft.com/en-us/ef/ef6/) | | | | Needs investigation. It might be implicitly supported based on the configured [database provider](https://docs.microsoft.com/en-us/ef/ef6/fundamentals/providers/). |
| [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) | | | | Needs investigation. It might be implicitly supported based on the configured [database provider](https://docs.microsoft.com/en-us/ef/core/providers/). |
| [Dapper](https://github.com/DapperLib/Dapper) | | | | Needs investigation. It might be implicitly supported based on the configured [database provider](https://github.com/DapperLib/Dapper#will-dapper-work-with-my-db-provider). |
| [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient) | Yes | | [MS SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-2019) | |
| [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient) | Yes | | | |
| [MySql.Data](https://dev.mysql.com/doc/connector-net/en/connector-net-introduction.html) | | | | This is the official [MySQL](https://dev.mysql.com/) library. |
| [MySqlConnector](https://mysqlconnector.net/) | | | | Seems to be the [recommended library for MariaDB](https://mariadb.com/kb/en/mysqlconnector-for-adonet/). |
| [Npgsql](https://www.npgsql.org/) | Yes | | [PostgreSQL](https://www.postgresql.org/) | |
| [Microsoft.Data.SqlLite](https://docs.microsoft.com/en-us/dotnet/standard/data/sqlite/?tabs=netcore-cli) | | | | |
| [MongoDB.Driver](https://www.nuget.org/packages/mongodb.driver) | Yes | | [MongoDB](https://www.mongodb.com/docs/) | |
| [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/) | | | | |
| [Elasticsearch-net](https://github.com/elastic/elasticsearch-net) | | | | We should be able to use [OpenTelemetry.Instrumentation.ElasticsearchClient](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.ElasticsearchClient). |
| [Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core) | | | | |
| [Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess) | | | | |
| [AWSSDK.DynamoDBv2](https://www.nuget.org/packages/AWSSDK.DynamoDBv2) | | | | |
| [Microsoft.Azure.Cosmos](https://www.nuget.org/packages/Microsoft.Azure.Cosmos) | | | | |

### Inter-process communication (IPC)

| Library | [Tracing Support](../config.md#instrumented-traces-libraries-and-frameworks) | [Metrics Support](../config.md#instrumented-metrics-libraries-and-frameworks) | Notes |
| --- | :---: | :---: | --- |
| [HttpClient](https://docs.microsoft.com/dotnet/api/system.net.http.httpclient) | Yes | Yes | |
| [HttpWebRequest](https://docs.microsoft.com/dotnet/api/system.net.httpwebrequest) | Yes | Yes | |
| [WCF](https://docs.microsoft.com/en-us/dotnet/framework/wcf/whats-wcf) | | | Server and client support should be added. |
| [CoreWCF](https://github.com/CoreWCF/CoreWCF) | | | Server and client support should be added. |
| [RestSharp](https://restsharp.dev/) | | | This library may be implicitly supported by instrumenting the underlying HttpClient or HttpWebRequest.  |
| [gRPC-dotnet](https://github.com/grpc/grpc-dotnet) | | | Client and service support should be added. |
| [GraphQL](https://www.nuget.org/packages/GraphQL/) | Yes | | The current instrumentation needs updates to match the semantic conventions. |
| [GraphQL Client](https://github.com/graphql-dotnet/graphql-client) | | | |
| [RabbitMQ](https://www.nuget.org/packages/RabbitMQ.Client) | | | These is an [issue in the RabbitMQ repo](https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/776) to add instrumentation directly into RabbitMQ. |
| [Kafka](https://www.nuget.org/packages/Confluent.Kafka) | | | |
| [NServiceBus](https://docs.particular.net/nservicebus/) | | | |
| [MassTransit](https://masstransit-project.com/) | | | |

### Logging

TODO: Determine how we want to support logging.

1. [Log4net](https://logging.apache.org/log4net/)
1. [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging/)
1. [Serilog](https://github.com/serilog/serilog)
1. [NLog](https://github.com/NLog/NLog)
1. [Common.Logging](https://github.com/net-commons/common-logging)

### Other

| Library | [Tracing Support](../config.md#instrumented-traces-libraries-and-frameworks) | [Metrics Support](../config.md#instrumented-metrics-libraries-and-frameworks) | Notes |
| --- | :---: | :---: | --- |
| [Microsoft.Extensions.Caching](https://docs.microsoft.com/en-us/dotnet/core/extensions/caching) | | | TODO: Evaluate if this is desired. |

## Cloud Environments and SDKs

### [AWS](https://aws.amazon.com/)

TODO: Determine appropriate resource detectors and AWS SDK related
instrumentation libraries to support.

### [Azure](https://azure.microsoft.com/)

TODO: Determine appropriate resource detectors and Azure SDK related
instrumentation libraries to support. This should also include support
for frameworks like [Azure Service Fabric](https://azure.microsoft.com/en-us/services/service-fabric/).

### [GCP](https://cloud.google.com/)

TODO: Determine appropriate resource detectors and GCP SDK related
instrumentation libraries to support.

### Libraries we should not support

| Library | Notes |
| --- | --- |
| [System.IO.Pipelines](https://docs.microsoft.com/en-us/dotnet/standard/io/pipelines) | This library is used for high-throughput scenarios and instrumenting this library could potentially impact the performance of the application in a negative manner. |
| [Pipelines.Sockets.Unofficial](https://github.com/mgravell/Pipelines.Sockets.Unofficial) | This is a lower-level networking library and instrumenting it can affect the performance of an application in negative ways causing an increase in network traffic (as compared to the application running without instrumentation). |
| [System.Threading.Channels](https://docs.microsoft.com/en-us/dotnet/api/system.threading.channels) | This is a lower-level library and may not provide enough contextual information on its own. |
| [Cassandra](https://www.nuget.org/packages/CassandraCSharpDriver) | Lower usage |
| [IBM DB2](https://www.ibm.com/docs/en/db2/11.5?topic=adonet-data-server-provider-net) | Lower usage |
| [Couchbase](https://www.nuget.org/packages/CouchbaseNetClient) | Lower usage |
| [LINQ to SQL](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/linq/) | Legacy technology and pattern. Wait until there is enough interest. |
| [Microsoft.Practices.EnterpriseLibrary.Data](https://docs.microsoft.com/en-us/previous-versions/msp-n-p/dn440726(v=pandp.60)) | Legacy technology and pattern. Wait until there is enough interest. |
| [Polly](http://www.thepollyproject.org/) | Waiting for interest and further analysis. |
| [gRPC for C#](https://github.com/grpc/grpc/tree/master/src/csharp) | Library is deprecated. |
| [MSMQ](https://docs.microsoft.com/en-us/previous-versions/windows/desktop/msmq/ms711472(v=vs.85)) | This is a legacy system. Wait until there is enough interest. |

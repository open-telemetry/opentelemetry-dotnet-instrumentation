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

1. ASP.NET
1. AspNetCore
1. Windows Workflow Foundation
1. OWIN
1. Azure Service Fabric

### Databases

#### General database frameworks

1. ADO.NET
1. EntityFramework
1. Dapper
1. LINQ to SQL
1. Microsoft.Practices.EnterpriseLibrary.Data
1. EntityFrameworkCore

#### Specific Databases

1. MySQL
    * MySql.data
    * MySqlConnector
1. PostgreSQL
1. SQLite
1. MongoDB
1. MS SQL Server
1. Redis
    * StackExchange.Redis
1. MariaDB
1. Firebase
1. Elasticsearch
1. Oracle
1. DynamoDB
1. Cosmos DB

### External request libraries (REST/RPC)

1. HttpClient
1. HttpWebRequest
1. WCF
1. RestSharp
1. Grpc
1. GraphQL
1. WCF Core

### Messaging

1. RabbitMQ
1. MSMQ
1. Kafka
1. NServiceBus

### Logging

1. Log4net
1. Microsoft.Extensions.Logging
1. Serilog
1. NLog
1. Common.Logging

### Other

1. Microsoft.extensions.caching

## Cloud Environments and SDKs

### AWS

TODO: Determine appropriate resource detectors and AWS SDK related
instrumentation libraries to support.

### Azure

TODO: Determine appropriate resource detectors and Azure SDK related
instrumentation libraries to support. This should also include support
for frameworks like [Azure Service Fabric](https://azure.microsoft.com/en-us/services/service-fabric/).

### GCP

TODO: Determine appropriate resource detectors and GCP SDK related
instrumentation libraries to support.

### Libraries we should not support

* System.IO.Pipelines - This library is used for high-throughput scenarios and
instrumenting this library could potentially impact the performance of the
application in a negative manner.
* Pipelines.Sockets.Unofficial - This is a lower-level networking library and
instrumenting it can affect the performance of an application in negative ways
causing an increase in network traffic (as compared to the application running
without instrumentation).
* System.Threading.Channels - This is a lower-level library and may not provide
enough contextual information on its own.
* Cassandra - Lower usage
* IBM DB2 - Lower usage
* Couchbase - Lower usage
* Polly - Waiting for interest and further analysis.

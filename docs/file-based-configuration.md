# File-based Configuration

> **Status:** [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md)
> For more information, see:
> **[Configuration SDK specification](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.49.0/specification/configuration/sdk.md)**  

You can configure OpenTelemetry using a YAML file.

To enable file-based configuration, set the following environment variable:

```bash
OTEL_EXPERIMENTAL_FILE_BASED_CONFIGURATION_ENABLED=true
```

By default, the value is false.

You can also specify the configuration file path (default: `config.yaml`):

```bash
OTEL_EXPERIMENTAL_CONFIG_FILE=/path/to/config.yaml
```

In your config file you can use environment variables in the format `${ENVIRONMENT_VARIABLE}`
instead of a fixed value.

You can also use `${ENVIRONMENT_VARIABLE:-value}`, where `value` is the fallback
if the environment variable is empty or not set.

For more details, see: [OpenTelemetry YAML File Format Specification](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.49.0/specification/configuration/data-model.md#yaml-file-format).

## Configuration Examples

### Resource Configuration

You can configure resource attributes directly in YAML or via the
`OTEL_RESOURCE_ATTRIBUTES` environment variable.

For more details and updates about Resource Detectors Configuration, see:
[Resource Detectors list and documentation](config.md/#resource-detectors)
To disable a resource detector, comment out or remove its corresponding entry.

``` yaml
resource:
  # Configure resource attributes. Entries have higher priority than entries from .resource.attributes_list.
  # Entries must contain .name and .value
  # By default service.name is generated automatically if not explicitly configured.
  # If the application is hosted on IIS in .NET Framework this will be SiteName\VirtualPath (e.g., MySite\MyApp).
  # Otherwise, it will use the name of the application entry Assembly.
  # You can override this value manually below.
  attributes:
    - name: service.name
      value: unknown_service
  # Alternatively, configure via a comma-separated list (same format as OTEL_RESOURCE_ATTRIBUTES).
  attributes_list: ${OTEL_RESOURCE_ATTRIBUTES}
  # Resource Detectors Configuration
  detection/development:
    # If no detectors are specified, none will be added automatically.
    detectors:
      azureappservice: # Detects Azure App Service resource information
      container:       # Detects container resource info (container.* attributes) [Core only]
      host:            # Detects host resource info (host.* attributes)
      operatingsystem: # Detects OS-level attributes (os.*)
      process:         # Detects process-level attributes (process.*)
      processruntime:  # Detects process runtime attributes (process.runtime.*)
```  

## Instrumentation Configuration

You can configure traces, metrics, and logs instrumentations.
For more details and updates, see: [Instrumentation list and documentation](config.md#instrumentations)

``` yaml
instrumentation/development:
  dotnet:
    traces:
      aspnet:              # ASP.NET (.NET Framework) MVC/WebApi [Framework only]
      aspnetcore:          # ASP.NET Core [Core only]
      azure:               # Azure SDK [Core & Framework]
      elasticsearch:       # Elastic.Clients.Elasticsearch [Core & Framework]
      elastictransport:    # Elastic.Transport (>=0.4.16) [Core & Framework]
      entityframeworkcore: # Entity Framework Core (>=6.0.12) [Core only]
      graphql:             # GraphQL (>=7.5.0) [Core only]
      grpcnetclient:       # Grpc.Net.Client (>=2.52.0 & <3.0.0) [Core & Framework]
      httpclient:          # System.Net.Http.HttpClient [Core & Framework]
      kafka:               # Confluent.Kafka (>=1.4.0 & <3.0.0) [Core & Framework]
      masstransit:         # MassTransit (>=8.0.0) [Core only]
      mongodb:             # MongoDB.Driver (>=2.7.0 <4.0.0) [Core & Framework]
      mysqlconnector:      # MySqlConnector (>=2.0.0) [Core only]
      mysqldata:           # MySql.Data (>=8.1.0) [Core only]
      npgsql:              # Npgsql (>=6.0.0) [Core only]
      nservicebus:         # NServiceBus (>=8.0.0 & <10.0.0) [Core & Framework]
      oraclemda:           # Oracle.ManagedDataAccess (>=23.4.0) [Core only]
      rabbitmq:            # RabbitMQ.Client (>=6.0.0) [Core & Framework]
      quartz:              # Quartz (>=3.4.0, not supported < .NET Framework 4.7.2)
      sqlclient:           # Microsoft.Data.SqlClient & System.Data.SqlClient [Core & Framework]
      stackexchangeredis:  # StackExchange.Redis (>=2.6.122 & <3.0.0) [Core only]
      wcfclient:           # WCF Client [Core & Framework]
      wcfservice:          # WCF Service [Framework only]
    metrics:
      aspnet:              # ASP.NET metrics [Framework only]
      aspnetcore:          # ASP.NET Core metrics [Core only]
      httpclient:          # HttpClient metrics [Core & Framework]
      netruntime:          # .NET Runtime metrics [Core only]
      nservicebus:         # NServiceBus metrics [Core & Framework]
      process:             # Process metrics [Core & Framework]
      sqlclient:           # SQL Client metrics [Core & Framework]
    logs:
      ilogger:             # Microsoft.Extensions.Logging (>=9.0.0) [Core & Framework]
      log4net:             # log4net (>=2.0.13 && <4.0.0) [Core & Framework]
```

## Instrumentation options

``` yaml
instrumentation/development:
  dotnet:
    traces:
      entityframeworkcore:
        # Whether the Entity Framework Core instrumentation can pass SQL statements through the db.statement attribute. Queries might contain sensitive information. If set to false, db.statement is recorded only for executing stored procedures.
        # Default is false
        set_db_statement_for_text: false
      graphql:
        # Whether the GraphQL instrumentation can pass raw queries through the graphql.document attribute. Queries might contain sensitive information.
        # Default is false
        set_document: false
      oraclemda: 
        # Whether the Oracle Client instrumentation can pass SQL statements through the db.statement attribute. Queries might contain sensitive information. If set to false, db.statement is recorded only for executing stored procedures.
        # Default is false
        set_db_statement_for_text: false
      sqlclient:
        # Whether the SQL Client instrumentation can pass SQL statements through the db.statement attribute. Queries might contain sensitive information. If set to false, db.statement is recorded only for executing stored procedures. 
        # Not supported on .NET Framework for System.Data.SqlClient.
        # Default is false
        set_db_statement_for_text: false
      aspnet:
        # A comma-separated list of HTTP header names. ASP.NET instrumentations will capture HTTP request header values for all configured header names.
        capture_request_headers: "X-Key"
        # A comma-separated list of HTTP header names. ASP.NET instrumentations will capture HTTP response header values for all configured header names.
        capture_response_headers: "X-Key"
      aspnetcore:
        # A comma-separated list of HTTP header names. ASP.NET Core instrumentations will capture HTTP request header values for all configured header names.
        capture_request_headers: "X-Key"
        # A comma-separated list of HTTP header names. ASP.NET Core instrumentations will capture HTTP response header values for all configured header names.
        capture_response_headers: "X-Key"
      httpclient:
        # A comma-separated list of HTTP header names. HTTP Client instrumentations will capture HTTP request header values for all configured header names.
        capture_request_headers: "X-Key"
        # A comma-separated list of HTTP header names. HTTP Client instrumentations will capture HTTP response header values for all configured header names.
        capture_response_headers: "X-Key"
      grpcnetclient:
        # A comma-separated list of gRPC metadata names. Grpc.Net.Client instrumentations will capture gRPC request metadata values for all configured metadata names.
        capture_request_metadata: "X-Key"
        # A comma-separated list of gRPC metadata names. Grpc.Net.Client instrumentations will capture gRPC response metadata values for all configured metadata names.
        capture_response_metadata: "X-Key"
```

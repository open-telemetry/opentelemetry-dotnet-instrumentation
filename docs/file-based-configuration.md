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

## General Configuration

``` yaml
# The file format version.
# The yaml format is documented at
# <https://github.com/open-telemetry/opentelemetry-configuration/tree/main/schema>
# By default, the latest version is used.
file_format: "1.0-rc.1"
# Configure if the SDK is disabled or not.
# If omitted or null, false is used
disabled: false
# Configure if the Fail Fast is enabled or not.
# If omitted or null, false is used
fail_fast: false
# Configure if the Flush On Unhandled Exception is enabled or not.
# If omitted or null, false is used.
flush_on_unhandled_exception: false
```

### Tracer Provider Configuration

``` yaml
tracer_provider:
  processors:
    # Batch processor for OTLP HTTP
    - batch:
      # Configure delay interval (in milliseconds) between two consecutive exports. 
      # Value must be non-negative.
      # If omitted or null, 5000 is used.
      schedule_delay: 5000
      # Configure maximum allowed time (in milliseconds) to export data. 
      # Value must be non-negative. A value of 0 indicates no limit (infinity).
      # If omitted or null, 30000 is used.
      export_timeout: 30000
      # Configure maximum queue size. Value must be positive.
      # If omitted or null, 2048 is used.
      max_queue_size: 2048
      # Configure maximum batch size. Value must be positive.
      # If omitted or null, 512 is used.
      max_export_batch_size: 512
      # Configure exporters.
      exporter:
        # Configure the OTLP with HTTP transport exporter to enable it.
        otlp_http:
          # Configure endpoint, including the trace specific path.
          # If omitted or null, http://localhost:4318/v1/traces is used
          endpoint: http://localhost:4318/v1/traces
          # Configure max time (in milliseconds) to wait for each export. 
          # Value must be non-negative. A value of 0 indicates no limit (infinity).
          # If omitted or null, 10000 is used.
          timeout: 10000
          # Configure headers. Entries have higher priority than entries from .headers_list.
          # If an entry's .value is null, the entry is ignored.
          headers:
            - name: api-key
              value: "1234"
          # Configure headers. Entries have lower priority than entries from .headers.
          # The value is a list of comma separated key-value pairs matching the format of OTEL_EXPORTER_OTLP_HEADERS. See https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/exporter.md#configuration-options for details.
          # If omitted or null, no headers are added.
          headers_list: ${OTEL_EXPORTER_OTLP_TRACES_HEADERS}

    # Batch processor for OTLP gRPC
    - batch:
        otlp_grpc:
          # Configuration otlp_grpc is the same as otlp_http.
          # if otlp_http is used it will override otlp_grpc.
          # On .NET Framework, the grpc OTLP exporter protocol is not supported.

    # Batch processor for Zipkin
    - batch:
        exporter:
          zipkin:
            # Configure endpoint.
            # If omitted or null, http://localhost:9411/api/v2/spans is used.
            endpoint: http://localhost:9411/api/v2/spans

    # Simple processor for Console
    - simple:
        exporter:
          console:
```

### Resource Configuration

You can configure resource attributes directly in YAML or via the
`OTEL_RESOURCE_ATTRIBUTES` environment variable.

For more details and updates about Resource Detectors Configuration, see:
[Resource Detectors list and documentation](config.md/#resource-detectors)
To disable a resource detector, comment out or remove its corresponding entry.

``` yaml
resource:
  # Configure resource attributes. Entries have higher priority than entries from .resource.attributes_list.
  # Entries must contain .name and .value, and may optionally include .type. If an entry's .type omitted or null, string is used.
  # The .value's type must match the .type. Values for .type include: string, bool, int, double, string_array, bool_array, int_array, double_array.
  # By default service.name is generated automatically if not explicitly configured.
  # If the application is hosted on IIS in .NET Framework this will be SiteName\VirtualPath (e.g., MySite\MyApp).
  # Otherwise, it will use the name of the application entry Assembly.
  # You can override this value manually below.
  attributes:
    - name: service.name
      value: unknown_service
      type: string
    - name: attribute_key
      value: ["value1", "value2", "value3"]
      type: string_array
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

### Propagator Configuration

You can configure text map context propagators directly in YAML or via the
`OTEL_PROPAGATORS` environment variable.
For more details and updates, see: [Propagators list and documentation](config.md/#propagators)
To disable a propagator, comment out or remove its corresponding entry.

``` yaml
# If no propagators are specified, none will be added automatically.
propagator:
  # Composite propagators are evaluated together. 
  # Entries from .composite_list are appended here (duplicates are filtered out).
  composite:
    tracecontext: # W3C Trace Context propagator
    baggage:      # W3C Baggage propagator
    b3:           # B3 single-header propagator
    b3multi:      # B3 multi-header propagator
  # Alternatively, configure via a comma-separated list (same format as OTEL_PROPAGATORS).
  composite_list: ${OTEL_PROPAGATORS}
```

## Instrumentation Configuration

You can configure traces, metrics, and logs instrumentations.
For more details and updates, see: [Instrumentation list and documentation](config.md#instrumentations)
To disable a instrumentation, comment out or remove its corresponding entry.

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
        capture_request_headers: "X-Key,X-Custom-Header,X-Header-Example"
        # A comma-separated list of HTTP header names. ASP.NET instrumentations will capture HTTP response header values for all configured header names.
        capture_response_headers: "X-Key,X-Custom-Header,X-Header-Example"
      aspnetcore:
        # A comma-separated list of HTTP header names. ASP.NET Core instrumentations will capture HTTP request header values for all configured header names.
        capture_request_headers: "X-Key,X-Custom-Header,X-Header-Example"
        # A comma-separated list of HTTP header names. ASP.NET Core instrumentations will capture HTTP response header values for all configured header names.
        capture_response_headers: "X-Key,X-Custom-Header,X-Header-Example"
      httpclient:
        # A comma-separated list of HTTP header names. HTTP Client instrumentations will capture HTTP request header values for all configured header names.
        capture_request_headers: "X-Key,X-Custom-Header,X-Header-Example"
        # A comma-separated list of HTTP header names. HTTP Client instrumentations will capture HTTP response header values for all configured header names.
        capture_response_headers: "X-Key,X-Custom-Header,X-Header-Example"
      grpcnetclient:
        # A comma-separated list of gRPC metadata names. Grpc.Net.Client instrumentations will capture gRPC request metadata values for all configured metadata names.
        capture_request_metadata: "X-Key,X-Custom-Header,X-Header-Example"
        # A comma-separated list of gRPC metadata names. Grpc.Net.Client instrumentations will capture gRPC response metadata values for all configured metadata names.
        capture_response_metadata: "X-Key,X-Custom-Header,X-Header-Example"
    logs:
      log4net:
        # Logs bridge is disabled by default
        # More info about log4net bridge can be found at https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/blob/main/docs/log4net-bridge.md
        bridge_enabled: true
```

### Configuration based instrumentation

Documentation for configuration based instrumentation can be found in [nocode-instrumentation.md](nocode-instrumentation.md).

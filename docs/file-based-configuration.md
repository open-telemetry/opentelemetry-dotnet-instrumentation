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

## Not Supported configurations

> ⚠️ **Important:**  
> File-based configuration support is currently **limited**.  
> Some options related to runtime behavior, process management, and .NET CLR profiler
> settings **cannot be configured via YAML files**.  
> These options **must still be set using environment variables** to
> ensure correct runtime behavior and instrumentation activation.

The settings listed below are **not supported** through the configuration
file and should continue to be configured via **environment variables**.

Future versions of the .NET Auto-Instrumentation may extend
file-based configuration to include these parameters.

### General

| Environment variable                                 | Description                                                                                                               |
| ---------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| `OTEL_DOTNET_AUTO_HOME`                              | Installation location.                                                                                                    |
| `OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES`                 | Names of the executable files that the profiler cannot instrument.                                                        |
| `OTEL_DOTNET_AUTO_OPENTRACING_ENABLED`               | Enables OpenTracing tracer.                                                                                               |
| `OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED`            | Enables automatic redirection of the assemblies used by the automatic instrumentation on the .NET Framework.              |
| `OTEL_DOTNET_AUTO_SQLCLIENT_NETFX_ILREWRITE_ENABLED` | Enables IL rewriting of `SqlCommand` on .NET Framework to ensure `CommandText` is present for `SqlClient` instrumentation |

---

### Limits

| Environment variable                          | Description                                                                                                  |
| --------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| `OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT`           | Maximum allowed attribute value size.                                                                        |
| `OTEL_ATTRIBUTE_COUNT_LIMIT`                  | Maximum allowed span attribute count.                                                                        |
| `OTEL_SPAN_ATTRIBUTE_VALUE_LENGTH_LIMIT`      | Maximum allowed attribute value size.                                                                        |
| `OTEL_SPAN_ATTRIBUTE_COUNT_LIMIT`             | Maximum allowed span attribute count.                                                                        |
| `OTEL_SPAN_EVENT_COUNT_LIMIT`                 | Maximum allowed span event count.                                                                            |
| `OTEL_SPAN_LINK_COUNT_LIMIT`                  | Maximum allowed span link count.                                                                             |
| `OTEL_EVENT_ATTRIBUTE_COUNT_LIMIT`            | Maximum allowed attribute per span event count.                                                              |
| `OTEL_LINK_ATTRIBUTE_COUNT_LIMIT`             | Maximum allowed attribute per span link count.                                                               |
| `OTEL_LOGRECORD_ATTRIBUTE_VALUE_LENGTH_LIMIT` | Maximum allowed log record attribute value size.                                                             |
| `OTEL_LOGRECORD_ATTRIBUTE_COUNT_LIMIT`        | Maximum allowed log record attribute count.                                                                  |

---

### Rule Engine

| Environment variable                   | Description         |
| -------------------------------------- | ------------------- |
| `OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED` | Enables RuleEngine. |

---

### .NET CLR Profiler

| .NET Framework variable  | .NET Core variable         | Description                                                                |
| ------------------------ | -------------------------- | -------------------------------------------------------------------------- |
| `COR_ENABLE_PROFILING`   | `CORECLR_ENABLE_PROFILING` | Enables the profiler.                                                      |
| `COR_PROFILER`           | `CORECLR_PROFILER`         | CLSID of the profiler.                                                     |
| `COR_PROFILER_PATH`      | `CORECLR_PROFILER_PATH`    | Path to the profiler.                                                      |
| `COR_PROFILER_PATH_32`   | `CORECLR_PROFILER_PATH_32` | Path to the 32-bit profiler. Bitness-specific paths take precedence.       |
| `COR_PROFILER_PATH_64`   | `CORECLR_PROFILER_PATH_64` | Path to the 64-bit profiler. Bitness-specific paths take precedence.       |

---

### .NET Runtime

| Environment variable     | Description                                          |
|--------------------------|------------------------------------------------------|
| `DOTNET_STARTUP_HOOKS`   | Specifies managed assemblies to load during startup. |
| `DOTNET_ADDITIONAL_DEPS` | Additional .deps.json files to include.              |
| `DOTNET_SHARED_STORE`    | Path to additional shared assemblies.                |

---

### Internal Logs

| Environment variable             | Description                                                                           |
|----------------------------------|---------------------------------------------------------------------------------------|
| `OTEL_DOTNET_AUTO_LOG_DIRECTORY` | Directory of the .NET Tracer logs.                                                    |
| `OTEL_LOG_LEVEL`                 | SDK log level.                                                                        |
| `OTEL_DOTNET_AUTO_LOGGER`        | Auto-Instrumentation diagnostic logs sink.                                            |
| `OTEL_DOTNET_AUTO_LOG_FILE_SIZE` | Maximum size (in bytes) of a single log file created by the Auto-Instrumentation.     |

---

### Instrumentations disable URL query redaction

| Environment variable                                              | Description                                                                                      |
| ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| `OTEL_DOTNET_EXPERIMENTAL_ASPNETCORE_DISABLE_URL_QUERY_REDACTION` | Whether the ASP.NET Core instrumentation turns off redaction of the `url.query` attribute value. |
| `OTEL_DOTNET_EXPERIMENTAL_HTTPCLIENT_DISABLE_URL_QUERY_REDACTION` | Whether the HTTP client instrumentation turns off redaction of the `url.full` attribute value.   |
| `OTEL_DOTNET_EXPERIMENTAL_ASPNET_DISABLE_URL_QUERY_REDACTION`     | Whether the ASP.NET instrumentation turns off redaction of the `url.query` attribute value.      |

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
# Configure if the Logs Include Formatted Message is enabled or not.
# If omitted or null, false is used.
logs_include_formatted_message: false
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
        exporter:
          # Configure exporter to be OTLP with gRPC transport.
          otlp_grpc:
            # Configuration otlp_grpc is the same as otlp_http.
            # On .NET Framework, the grpc OTLP exporter protocol is not supported.

    # Batch processor for Zipkin
    # This configuration is deprecated and will be removed in upcoming release.
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

### Meter Provider Configuration

``` yaml
meter_provider:
  readers:
    # Periodic reader for OTLP HTTP
    - periodic:
        # Configure delay interval (in milliseconds) between start of two consecutive exports.
        # Value must be non-negative.
        # If omitted or null, 60000 is used.
        interval: 60000
        # Configure maximum allowed time (in milliseconds) to export data.
        # Value must be non-negative. A value of 0 indicates no limit (infinity).
        # If omitted or null, 30000 is used.
        timeout: 30000
        # Configure exporter.
        exporter:
          # Configure exporter to be OTLP with HTTP transport.
          otlp_http:
            # Configure endpoint, including the metric specific path.
            # If omitted or null, http://localhost:4318/v1/metrics is used.
            endpoint: http://localhost:4318/v1/metrics
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
            # Configure temporality preference.
            # Values include: cumulative, delta.
            # If omitted or null, cumulative is used.
            temporality_preference: cumulative

    # Periodic reader for OTLP gRPC
    - periodic:
        # Configure exporter.
        exporter:
          # Configure exporter to be OTLP with gRPC transport.
          otlp_grpc:
          # Configuration otlp_grpc is the same as otlp_http.
          # On .NET Framework, the grpc OTLP exporter protocol is not supported.

    # Periodic reader for Console
    - periodic:
        # Configure exporter.
        exporter:
          # Configure exporter to be console.
          console:

    # Pull reader for Prometheus
    - pull:
        # Configure exporter.
        exporter:
          # Configure exporter to be Prometheus.
          prometheus:
```

### Logger Provider Configuration

``` yaml
logger_provider:
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
        # Configure exporter.
        exporter:
          # Configure the OTLP with HTTP transport exporter to enable it.
          otlp_http:
            # Configure endpoint, including the logs specific path.
            # If omitted or null, http://localhost:4318/v1/logs is used.
            endpoint: http://localhost:4318/v1/logs
            # Configure headers. Entries have higher priority than entries from .headers_list.
            # If an entry's .value is null, the entry is ignored.
            headers:
              - name: api-key
                value: "1234"
            # Configure headers. Entries have lower priority than entries from .headers.
            # The value is a list of comma separated key-value pairs matching the format of OTEL_EXPORTER_OTLP_HEADERS.
            # If omitted or null, no headers are added.
            headers_list: api-key=1234
            # Configure max time (in milliseconds) to wait for each export.
            # Value must be non-negative. A value of 0 indicates no limit (infinity).
            # If omitted or null, 10000 is used.
            timeout: 10000

    # Batch processor for OTLP gRPC
    - batch:
        exporter:
          # Configure exporter to be OTLP with gRPC transport.
          otlp_grpc:
          # Configuration otlp_grpc is the same as otlp_http.
          # On .NET Framework, the grpc OTLP exporter protocol is not supported.

    # Simple processor for Console
    - simple:
        exporter:
          console:
```

### Sampler Configuration

1. Parent Based Always On Sampler Configuration

   ``` yaml
   tracer_provider:
     # Configure the sampler. If omitted, parent based sampler with a root of always_on is used.
     sampler:
       # Configure sampler to be parent_based.
       parent_based:
         # Configure root sampler.
         # If omitted or null, always_on is used.
         root:
           # Configure sampler to be always_on.
           always_on:
         # Configure remote_parent_sampled sampler.
         # If omitted or null, always_on is used.
         remote_parent_sampled:
           # Configure sampler to be always_on.
           always_on:
         # Configure remote_parent_not_sampled sampler.
         # If omitted or null, always_off is used.
         remote_parent_not_sampled:
           # Configure sampler to be always_off.
           always_off:
         # Configure local_parent_sampled sampler.
         # If omitted or null, always_on is used.
         local_parent_sampled:
           # Configure sampler to be always_on.
           always_on:
         # Configure local_parent_not_sampled sampler.
         # If omitted or null, always_off is used.
         local_parent_not_sampled:
           # Configure sampler to be always_off.
           always_off:
   ```

2. Parent Based Trace Id Ratio Sampler Configuration

   ``` yaml
   tracer_provider:
     # Configure the sampler. If omitted, parent based sampler with a root of always_on is used.
     sampler:
       # Configure sampler to be parent_based.
       parent_based:
         # Configure root sampler.
         # If omitted or null, always_on is used.
         root:
           # Configure sampler to be always_on.
           trace_id_ratio:
             # Configure ratio between 0.0 and 1.0.
             ratio: 0.25
   ```

3. Always On Sampler Configuration

   ``` yaml
   tracer_provider:
     # Configure the sampler. If omitted, parent based sampler with a root of always_on is used.
     sampler:
       # Configure sampler to be always_on.
       always_on:
   ```

4. Always Off Sampler Configuration

   ``` yaml
   tracer_provider:
     # Configure the sampler. If omitted, parent based sampler with a root of always_on is used.
     sampler:
       # Configure sampler to be always_off.
       always_off:
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

### Plugins Configuration

For more details and updates about Plugins Configuration, see:
[Plugins documentation](plugins.md)

``` yaml
plugins/development:
  # Configure plugins. Entries have higher priority than entries from .plugins_list.
  # List of plugins to load. Each entry is the full type name, followed by the assembly name.
  # For example: MyNamespace.MyPlugin, MyAssembly, Version=1.0.0, Culture=neutral, PublicKeyToken=null
  plugins:
    - Test1.Plugins.Plugin, Test1.Plugins, Version=1.0.0, Culture=neutral, PublicKeyToken=null
    - Test2.Plugins.Plugin, Test2.Plugins
  # Alternatively, configure via a colon-separated list (same format as OTEL_DOTNET_AUTO_PLUGINS).
  plugins_list: ${OTEL_DOTNET_AUTO_PLUGINS}
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
Each entry in the example list below is equivalent to setting the corresponding
environment variable described in the [Instrumentation list and documentation](config.md#instrumentations).
To disable a instrumentation, comment out or remove its corresponding entry.

``` yaml
instrumentation/development:
  dotnet:
    traces:
      aspnet:              # ASP.NET
      aspnetcore:          # ASP.NET Core
      azure:               # Azure SDK
      elasticsearch:       # Elastic.Clients.Elasticsearch
      elastictransport:    # Elastic.Transport
      entityframeworkcore: # Entity Framework Core
      graphql:             # GraphQL
      grpcnetclient:       # Grpc.Net.Client
      httpclient:          # System.Net.Http.HttpClient
      kafka:               # Confluent.Kafka
      masstransit:         # MassTransit
      mongodb:             # MongoDB.Driver
      mysqlconnector:      # MySqlConnector
      mysqldata:           # MySql.Data
      npgsql:              # Npgsql
      nservicebus:         # NServiceBus
      oraclemda:           # Oracle.ManagedDataAccess
      rabbitmq:            # RabbitMQ.Client
      quartz:              # Quartz
      sqlclient:           # Microsoft.Data.SqlClient & System.Data.SqlClient
      stackexchangeredis:  # StackExchange.Redis
      wcfclient:           # WCF Client
      wcfservice:          # WCF Service
    metrics:
      aspnet:              # ASP.NET metrics
      aspnetcore:          # ASP.NET Core metrics
      httpclient:          # HttpClient metrics
      netruntime:          # .NET Runtime metrics
      nservicebus:         # NServiceBus metrics
      process:             # Process metrics
      sqlclient:           # SQL Client metrics
    logs:
      ilogger:             # Microsoft.Extensions.Logging
      log4net:             # Log4Net
```

## Instrumentation options

``` yaml
instrumentation/development:
  dotnet:
    traces:
      graphql:
        # Whether the GraphQL instrumentation can pass raw queries through the graphql.document attribute. Queries might contain sensitive information.
        # Default is false
        set_document: false
      oraclemda: 
        # Whether the Oracle Client instrumentation can pass SQL statements through the db.statement attribute. Queries might contain sensitive information. If set to false, db.statement is recorded only for executing stored procedures.
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

## Instrumentation Additional Sources

``` yaml
instrumentation/development:
  dotnet:
    traces:
      # List of additional System.Diagnostics.ActivitySource names to be added to the tracer at the startup. 
      # Use it to capture manually instrumented spans.
      additional_sources:
        - Another.Source.Name
      # Alternatively, configure via a comma-separated list (same format as OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES).
      additional_sources_list: ${OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES}
      # List of additional legacy source names to be added to the tracer at the startup. 
      # Use it to capture System.Diagnostics.Activity objects created without using the System.Diagnostics.ActivitySource API.
      additional_legacy_sources:
        - Legacy.Source.Name
      # Alternatively, configure via a comma-separated list (same format as OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES).
      additional_legacy_sources_list: ${OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES}
    metrics:
      # List of additional System.Diagnostics.Metrics.Meter names to be added to the meter at the startup. 
      additional_sources:
        - MyProduct.MyLibrary.Metrics
      # Alternatively, configure via a comma-separated list (same format as OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES).
      additional_sources_list: ${OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES}
```

### Configuration based instrumentation

Documentation for configuration based instrumentation can be found in [nocode-instrumentation.md](nocode-instrumentation.md).

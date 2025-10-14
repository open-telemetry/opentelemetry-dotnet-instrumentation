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

### Configuration based instrumentation

Documentation for configuration based instrumentation can be found in [nocode-instrumentation.md](nocode-instrumentation.md).

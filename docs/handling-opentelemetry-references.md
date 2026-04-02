# Add automatic instrumentation to application with existing OpenTelemetry SDK

This document explains how OpenTelemetry automatic instrumentation interacts with
applications that already include references to the OpenTelemetry SDK or instrumentation
libraries. It covers the role and impact of the `OTEL_DOTNET_AUTO_SETUP_SDK`
environment variable and provides troubleshooting guidance for conflicts between
automatic instrumentation and existing references. The objective is to help
users understand and effectively integrate OpenTelemetry automatic
instrumentation into their applications.

## Interaction with existing OpenTelemetry SDK and instrumentation references

OpenTelemetry automatic instrumentation enhances .NET applications by
automatically injecting and configuring the OpenTelemetry .NET SDK and adding
OpenTelemetry instrumentation to key packages and APIs that the application uses.
For applications without existing OpenTelemetry references, the process is seamless.
However, for applications that already include OpenTelemetry SDK or
instrumentation libraries, the interaction requires careful management to avoid
conflicts.

### The `OTEL_DOTNET_AUTO_SETUP_SDK` environment variable

The `OTEL_DOTNET_AUTO_SETUP_SDK` environment variable plays a crucial role in
managing the interaction between OpenTelemetry automatic instrumentation and existing
OpenTelemetry references within an application. When set to `true`, this
environment variable instructs the automatic instrumentation to automatically
set up the OpenTelemetry SDK, ensuring that it is properly configured to work
with the instrumentation injected by the automatic instrumentation.

This automatic setup is particularly useful in scenarios where the application
has its own references to OpenTelemetry libraries, as it helps to harmonize the
configuration and ensure that telemetry data is collected and exported as
expected.

### Impact on applications

The presence of the `OTEL_DOTNET_AUTO_SETUP_SDK` environment variable and its
configuration can significantly impact how telemetry data is collected and
exported in applications with existing OpenTelemetry references. When enabled,
it ensures that the OpenTelemetry SDK is configured in a way that complements
the automatic instrumentation, thereby enhancing the telemetry data collected
without causing conflicts or duplication.

### Troubleshooting

When there are conflicts within an application between the OpenTelemetry
automatic instrumentation and existing OpenTelemetry references, try
these troubleshooting steps:

1. Verify that the `OTEL_DOTNET_AUTO_SETUP_SDK` environment variable is set to
   `true` to allow the automatic instrumentation to manage the SDK setup.
2. Review the application's dependencies to identify any conflicting versions of
   OpenTelemetry libraries and resolve them by aligning with the versions used
   by the automatic instrumentation.
3. Consult the [troubleshooting guide](troubleshooting.md) for additional advice
   on resolving specific issues related to OpenTelemetry automatic instrumentation.

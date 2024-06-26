# Handling OpenTelemetry SDK/Instrumentation References in Applications with Auto-Instrumentation

## Summary

This document aims to provide guidance on how OpenTelemetry Auto-Instrumentation
interacts with applications that already include references to OpenTelemetry SDK
or Instrumentation libraries. It will cover the role and impact of the
`OTEL_DOTNET_AUTO_SETUP_SDK` environment variable and offer troubleshooting
advice. The objective is to help users understand and effectively manage the
integration of OpenTelemetry Auto-Instrumentation in their applications.

## Interaction with Existing OpenTelemetry SDK/Instrumentation References

OpenTelemetry Auto-Instrumentation is designed to enhance .NET applications by
automatically injecting and configuring the OpenTelemetry .NET SDK and adding
OpenTelemetry Instrumentation to key packages and APIs used by the application.
This process is seamless for applications without existing OpenTelemetry
references. However, for applications that already include OpenTelemetry SDK or
Instrumentation libraries, the interaction requires careful management to avoid
conflicts.

### The Role of `OTEL_DOTNET_AUTO_SETUP_SDK`

The `OTEL_DOTNET_AUTO_SETUP_SDK` environment variable plays a crucial role in
managing the interaction between OpenTelemetry Auto-Instrumentation and existing
OpenTelemetry references within an application. When set to `true`, this
environment variable instructs the Auto-Instrumentation to automatically set up
the OpenTelemetry SDK, ensuring that it is properly configured to work with the
instrumentation injected by the Auto-Instrumentation.

This automatic setup is particularly useful in scenarios where the application
has its own references to OpenTelemetry libraries, as it helps to harmonize the
configuration and ensure that telemetry data is collected and exported as
expected.

### Impact on Applications

The presence of the `OTEL_DOTNET_AUTO_SETUP_SDK` environment variable and its
configuration can significantly impact how telemetry data is collected and
exported in applications with existing OpenTelemetry references. When enabled,
it ensures that the OpenTelemetry SDK is configured in a way that complements
the automatic instrumentation, thereby enhancing the telemetry data collected
without causing conflicts or duplication.

### Troubleshooting

In cases where conflicts arise between the OpenTelemetry Auto-Instrumentation
and existing OpenTelemetry references within an application, the following
troubleshooting steps are recommended:

1. Ensure that the `OTEL_DOTNET_AUTO_SETUP_SDK` environment variable is set to
   `true` to allow the Auto-Instrumentation to manage the SDK setup.
2. Review the application's dependencies to identify any conflicting versions of
   OpenTelemetry libraries and resolve them by aligning with the versions used
   by the Auto-Instrumentation.
3. Consult the [troubleshooting guide](troubleshooting.md) for additional advice
   on resolving specific issues related to OpenTelemetry Auto-Instrumentation.

## Conclusion

Understanding the interaction between OpenTelemetry Auto-Instrumentation and
existing OpenTelemetry SDK or Instrumentation references is crucial for
effectively managing telemetry data collection in .NET applications. By
following the guidance provided in this document and leveraging the
`OTEL_DOTNET_AUTO_SETUP_SDK` environment variable, users can ensure a smooth
integration and maximize the benefits of automatic instrumentation.

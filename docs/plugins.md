# Plugins

You can use `OTEL_DOTNET_AUTO_PLUGINS` environment variable to extend the
configuration and overwrite options of the OpenTelemetry .NET SDK Tracer, Meter or
Logs. A plugin must be a non-static, non-abstract class which has a default constructor
and that implements at least one of the configuration methods below showed
in an example plugin class:

```csharp

public class MyPlugin 
{
    // To configure tracing SDK
    public OpenTelemetry.Trace.TracerProviderBuilder ConfigureTracerProvider(OpenTelemetry.Trace.TracerProviderBuilder builder)
    {
        // My custom logic here

        return builder;
    }

    // To configure metrics SDK
    public OpenTelemetry.Metrics.MeterProviderBuilder ConfigureMeterProvider(OpenTelemetry.Metrics.MeterProviderBuilder builder)
    {
        // My custom logic here

        return builder;
    }

    // To configure logs SDK
    public void ConfigureOptions(OpenTelemetry.Logs.OpenTelemetryLoggerOptions options)
    {
        // My custom logic here
    }

    // To configure any options used by OpenTelemetry .NET Automatic Instrumentation
    public void ConfigureOptions(OpenTelemetry.NameSpace.OptionType options)
    {
        // My custom logic here
        // Find supported options below
    }
}
```

## Supported Options

### Tracing

| Options type                                                                 | NuGet package                                | NuGet version |
|------------------------------------------------------------------------------|----------------------------------------------|---------------|
| OpenTelemetry.Exporter.ConsoleExporterOptions                                | OpenTelemetry.Exporter.Console               | 1.4.0-beta.3  |
| OpenTelemetry.Exporter.ZipkinExporterOptions                                 | OpenTelemetry.Exporter.Zipkin                | 1.4.0-beta.3  |
| OpenTelemetry.Exporter.JaegerExporterOptions                                 | OpenTelemetry.Exporter.Jaeger                | 1.4.0-beta.3  |
| OpenTelemetry.Exporter.OtlpExporterOptions                                   | OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.4.0-beta.3  |
| OpenTelemetry.Instrumentation.AspNet.AspNetInstrumentationOptions            | OpenTelemetry.Instrumentation.AspNet         | 1.0.0-rc9.7   |
| OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreInstrumentationOptions    | OpenTelemetry.Instrumentation.AspNetCore     | 1.0.0-rc9.9   |
| OpenTelemetry.Instrumentation.GrpcNetClient.GrpcClientInstrumentationOptions | OpenTelemetry.Instrumentation.GrpcNetClient  | 1.0.0-rc9.9   |
| OpenTelemetry.Instrumentation.Http.HttpClientInstrumentationOptions          | OpenTelemetry.Instrumentation.Http           | 1.0.0-rc9.9   |
| OpenTelemetry.Instrumentation.SqlClient.SqlClientInstrumentationOptions      | OpenTelemetry.Instrumentation.SqlClient      | 1.0.0-rc9.9   |
| OpenTelemetry.Instrumentation.MySqlData.MySqlDataInstrumentationOptions      | OpenTelemetry.Instrumentation.MySqlData      | 1.0.0-beta.4  |
| OpenTelemetry.Instrumentation.Wcf.WcfInstrumentationOptions                  | OpenTelemetry.Instrumentation.Wcf            | 1.0.0-rc7     |

### Metrics

| Options type                                                        | NuGet package                                  | NuGet version |
|---------------------------------------------------------------------|------------------------------------------------|---------------|
| OpenTelemetry.Metrics.MetricReaderOptions                           | OpenTelemetry                                  | 1.4.0-beta.3  |
| OpenTelemetry.Exporter.ConsoleExporterOptions                       | OpenTelemetry.Exporter.Console                 | 1.3.1         |
| OpenTelemetry.Exporter.PrometheusExporterOptions                    | OpenTelemetry.Exporter.Prometheus.HttpListener | 1.4.0-beta.3  |
| OpenTelemetry.Exporter.OtlpExporterOptions                          | OpenTelemetry.Exporter.OpenTelemetryProtocol   | 1.4.0-beta.3  |
| OpenTelemetry.Exporter.OtlpExporterOptions                          | OpenTelemetry.Exporter.OpenTelemetryProtocol   | 1.4.0-beta.3  |
| OpenTelemetry.Instrumentation.Runtime.RuntimeInstrumentationOptions | OpenTelemetry.Instrumentation.Runtime          | 1.1.0-beta.1  |
| OpenTelemetry.Instrumentation.Process.ProcessInstrumentationOptions | OpenTelemetry.Instrumentation.Process          | 1.0.0-alpha.2 |

### Logs

| Options type                                  | NuGet package                                | NuGet version |
|-----------------------------------------------|----------------------------------------------|---------------|
| OpenTelemetry.Logs.OpenTelemetryLoggerOptions | OpenTelemetry                                | 1.4.0-beta.3  |
| OpenTelemetry.Exporter.ConsoleExporterOptions | OpenTelemetry.Exporter.Console               | 1.4.0-beta.3  |
| OpenTelemetry.Exporter.OtlpExporterOptions    | OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.4.0-beta.3  |

## Requirements

* The plugin must use the same version of the `OpenTelemetry` as the
OpenTelemetry .NET Automatic Instrumentation.
* The plugin must use the same options versions as the
OpenTelemetry .NET Automatic Instrumentation (found in the table above).

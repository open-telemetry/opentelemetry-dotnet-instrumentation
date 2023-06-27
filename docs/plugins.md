# Plugins

You can use `OTEL_DOTNET_AUTO_PLUGINS` environment variable to extend the
configuration and overwrite options of the OpenTelemetry .NET SDK Tracer, Meter or
Logs. A plugin must be a non-static, non-abstract class which has a default constructor
and that implements at least one of the configuration methods below showed
in an example plugin class:

```csharp

public class MyPlugin 
{
    // To configure plugin, before OTel SDK configuration is called.
    public void Initializing()
    {
        // My custom logic here
    }

    // To configure tracing SDK
    public OpenTelemetry.Trace.TracerProviderBuilder ConfigureTracerProvider(OpenTelemetry.Trace.TracerProviderBuilder builder)
    {
        // My custom logic here

        return builder;
    }
        
    // To configure any traces options used by OpenTelemetry .NET Automatic Instrumentation
    public void ConfigureTracesOptions(OpenTelemetry.NameSpace.OptionType options)
    {
        // My custom logic here
        // Find supported options below
    }

    // To configure metrics SDK
    public OpenTelemetry.Metrics.MeterProviderBuilder ConfigureMeterProvider(OpenTelemetry.Metrics.MeterProviderBuilder builder)
    {
        // My custom logic here

        return builder;
    }
    
    // To configure any metrics options used by OpenTelemetry .NET Automatic Instrumentation
    public void ConfigureMetricsOptions(OpenTelemetry.NameSpace.OptionType options)
    {
        // My custom logic here
        // Find supported options below
    }

    // To configure logs SDK (the method name is the same as for other logs options)
    public void ConfigureLogsOptions(OpenTelemetry.Logs.OpenTelemetryLoggerOptions options)
    {
        // My custom logic here
    }

    // To configure any logs options used by OpenTelemetry .NET Automatic Instrumentation
    public void ConfigureLogsOptions(OpenTelemetry.NameSpace.OptionType options)
    {
        // My custom logic here
        // Find supported options below
    }

    // To configure Resource
    public OpenTelemetry.Resources.ResourceBuilder ConfigureResource(OpenTelemetry.Resources.ResourceBuilder builder)
    {
        // My custom logic here
        // Please note this method is common to set the resource for trace, logs and metrics.
        // This method could be overridden by ConfigureTracesOptions, ConfigureMeterProvider and ConfigureLogsOptions
        // by calling SetResourceBuilder with new object.

        return builder;
    }
}
```

## Supported Options

### Tracing

| Options type                                                                              | NuGet package                                     | NuGet version |
|-------------------------------------------------------------------------------------------|---------------------------------------------------|---------------|
| OpenTelemetry.Exporter.ConsoleExporterOptions                                             | OpenTelemetry.Exporter.Console                    | 1.5.1         |
| OpenTelemetry.Exporter.ZipkinExporterOptions                                              | OpenTelemetry.Exporter.Zipkin                     | 1.5.1         |
| OpenTelemetry.Exporter.OtlpExporterOptions                                                | OpenTelemetry.Exporter.OpenTelemetryProtocol      | 1.5.1         |
| OpenTelemetry.Instrumentation.AspNet.AspNetInstrumentationOptions                         | OpenTelemetry.Instrumentation.AspNet              | 1.0.0-rc9.9   |
| OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreInstrumentationOptions                 | OpenTelemetry.Instrumentation.AspNetCore          | 1.5.0-beta.1  |
| OpenTelemetry.Instrumentation.EntityFrameworkCore.EntityFrameworkInstrumentationOptions   | OpenTelemetry.Instrumentation.EntityFrameworkCore | 1.0.0-beta.7  |
| OpenTelemetry.Instrumentation.GrpcNetClient.GrpcClientInstrumentationOptions              | OpenTelemetry.Instrumentation.GrpcNetClient       | 1.5.0-beta.1  |
| OpenTelemetry.Instrumentation.Http.HttpClientInstrumentationOptions                       | OpenTelemetry.Instrumentation.Http                | 1.5.0-beta.1  |
| OpenTelemetry.Instrumentation.Quartz.QuartzInstrumentationOptions                         | OpenTelemetry.Instrumentation.Quartz              | 1.0.0-alpha.3 |
| OpenTelemetry.Instrumentation.SqlClient.SqlClientInstrumentationOptions                   | OpenTelemetry.Instrumentation.SqlClient           | 1.5.0-beta.1  |
| OpenTelemetry.Instrumentation.StackExchangeRedis.StackExchangeRedisInstrumentationOptions | OpenTelemetry.Instrumentation.StackExchangeRedis  | 1.0.0-rc9.10  |
| OpenTelemetry.Instrumentation.MySqlData.MySqlDataInstrumentationOptions                   | OpenTelemetry.Instrumentation.MySqlData           | 1.0.0-beta.7  |
| OpenTelemetry.Instrumentation.Wcf.WcfInstrumentationOptions                               | OpenTelemetry.Instrumentation.Wcf                 | 1.0.0-rc.10   |

### Metrics

| Options type                                                                     | NuGet package                                  | NuGet version |
|----------------------------------------------------------------------------------|------------------------------------------------|---------------|
| OpenTelemetry.Metrics.MetricReaderOptions                                        | OpenTelemetry                                  | 1.5.1         |
| OpenTelemetry.Exporter.ConsoleExporterOptions                                    | OpenTelemetry.Exporter.Console                 | 1.5.1         |
| OpenTelemetry.Exporter.PrometheusExporterOptions                                 | OpenTelemetry.Exporter.Prometheus.HttpListener | 1.5.0-rc.1    |
| OpenTelemetry.Exporter.OtlpExporterOptions                                       | OpenTelemetry.Exporter.OpenTelemetryProtocol   | 1.5.1         |
| OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreMetricsInstrumentationOptions | OpenTelemetry.Instrumentation.AspNetCore       | 1.5.0-beta.1  |
| OpenTelemetry.Instrumentation.Runtime.RuntimeInstrumentationOptions              | OpenTelemetry.Instrumentation.Runtime          | 1.5.0         |

### Logs

| Options type                                  | NuGet package                                | NuGet version |
|-----------------------------------------------|----------------------------------------------|---------------|
| OpenTelemetry.Logs.OpenTelemetryLoggerOptions | OpenTelemetry                                | 1.5.0-rc.1    |
| OpenTelemetry.Exporter.ConsoleExporterOptions | OpenTelemetry.Exporter.Console               | 1.5.1         |
| OpenTelemetry.Exporter.OtlpExporterOptions    | OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.5.1         |

## Requirements

* The plugin must use the same version of the `OpenTelemetry` as the
OpenTelemetry .NET Automatic Instrumentation.
* The plugin must use the same options versions as the
OpenTelemetry .NET Automatic Instrumentation (found in the table above).

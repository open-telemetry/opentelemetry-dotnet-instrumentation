# Usage

**WARNING**: Please notice that no official release has been created for this repo yet, so installation instructions below would require you to build the code manually beforehand.

## Configure the OpenTelemetry Tracing Library for .NET

### Configuration values

Use these environment variables to configure the tracing library:

| Environment variable | Default value | Description |
|-|-|-|
| `OTEL_SERVICE_NAME` |  | The name of the service. |
| `OTEL_ENV` |  | The value for the `environment` tag added to every span. This determines the environment in which the service is available in OTEL µAPM.  |
| `OTEL_TRACE_ENABLED` | `true` | Enable to activate the tracer. |
| `OTEL_TRACE_DEBUG` | `false` | Enable to activate debugging mode for the tracer. |
| `OTEL_TRACE_AGENT_URL` | `http://localhost:9080/v1/trace` | The hostname and port for a OTEL Smart Agent or OpenTelemetry Collector. |
| `OTEL_TRACE_GLOBAL_TAGS` |  | Comma-separated list of key-value pairs to specify global span tags. For example: `"key1:val1,key2:val2"` |
| `OTEL_LOGS_INJECTION` | `false` | Enable to inject trace IDs, span IDs, service name and environment into logs. This requires a compatible logger or manual configuration. |
| `OTEL_MAX_LOGFILE_SIZE` | `10 MB` | The maximum size for tracer log files, in bytes. |
| `OTEL_TRACE_LOG_PATH` | Linux: `/var/log/OTEL/dotnet/dotnet-profiler.log`<br>Windows: `%ProgramData%"\OTEL .NET Tracing\logs\dotnet-profiler.log` | The path of the profiler log file. |
| `OTEL_DIAGNOSTIC_SOURCE_ENABLED` | `true` | Enable to generate troubleshooting logs with the `System.Diagnostics.DiagnosticSource` class. |
| `OTEL_DISABLED_INTEGRATIONS` |  | The integrations you want to disable, if any, separated by a semi-colon. These are the supported integrations: AspNetMvc, AspNetWebApi2, DbCommand, ElasticsearchNet5, ElasticsearchNet6, GraphQL, HttpMessageHandler, IDbCommand, MongoDb, NpgsqlCommand, OpenTracing, ServiceStackRedis, SqlCommand, StackExchangeRedis, Wcf, WebRequest |
| `OTEL_TRACE_DOMAIN_NEUTRAL_INSTRUMENTATION` | `false` |  Sets whether to intercept method calls when the caller method is inside a domain-neutral assembly. This is recommended when instrumenting IIS applications. |
| `OTEL_PROFILER_PROCESSES` |  | Sets the filename of executables the profiler can attach to. If not defined (default), the profiler will attach to any process. Supports multiple values separated with semi-colons, for example: `MyApp.exe;dotnet.exe` |
| `OTEL_PROFILER_EXCLUDE_PROCESSES` |  | Sets the filename of executables the profiler cannot attach to. If not defined (default), the profiler will attach to any process. Supports multiple values separated with semi-colons, for example: `MyApp.exe;dotnet.exe` |
| `OTEL_CONVENTION` | `Datadog` | Sets the outbound http and trace id convention for tracer. Available values are: `Datadog` (64bit trace id), `OpenTelemetry` (128 bit trace id). |
| `OTEL_PROPAGATOR` | `Datadog` | Sets the propagator for tracer. Available values are: `Datadog`, `B3`, `W3C`. |

### Linux

After downloading the library, install the CLR Profiler and its components
via your system's package manager.

1. Download the latest release of the library.
2. Install the CLR Profiler and its components with your system's package
manager:
    ```bash
    # Use dpkg:
    $ dpkg -i otel-dotnet-autoinstrumentation.deb

    # Use rpm:
    $ rpm -ivh otel-dotnet-autoinstrumentation.rpm

    # Install directly from the release bundle:
    $ tar -xf otel-dotnet-autoinstrumentation.tar.gz -C /

    # Install directly from the release bundle for musl-using systems (Alpine Linux):
    $ tar -xf otel-dotnet-autoinstrumentation-musl.tar.gz -C /
    ```
1. Configure the required environment variables:
    ```bash
    $ source /opt/otel-dotnet-autoinstrumentation/defaults.env
    ```
2. Set the service name:
    ```bash
    $ export OTEL_SERVICE_NAME='MyCoreService'
    ```
3. Set Zipkin exporter:
    ```bash
    $ export OTEL_EXPORTER='Zipkin'
    ```
4. Set OpenTelemetry conventions:
    ```bash
    $ export OTEL_CONVENTION='OpenTelemetry'
    ```
5. Set the endpoint of a Smart Agent or OpenTelemetry Collector:
    ```bash
    $ export OTEL_TRACE_AGENT_URL='http://<YourSmartAgentOrCollector>:9080/v1/trace'
    ```
6. Optionally, enable trace injection in logs:
    ```bash
    $ export OTEL_LOGS_INJECTION=true
    ```
7. Optionally, create the default logging directory:
    ```bash
    $ source /opt/otel-dotnet-autoinstrumentation/createLogPath.sh
    ```
8. Run your application:
    ```bash
    $ dotnet run
    ```

### Windows

**Warning**: Pay close attention to the scope of environment variables. Ensure they
are properly set prior to launching the targeted process. The following steps set the
environment variables at the machine level, with the exception of the variables used
for finer control of which processes will be instrumented, which are set in the current
command session.

1. Install the CLR Profiler using an installer file (`.msi` file) from the latest release.
Choose the installer (x64 or x86) according to the architecture of the application
you're instrumenting.
2. Configure the required environment variables to enable the CLR Profiler:
    - For .NET Framework applications:
    ```batch
    setx COR_PROFILER "{918728DD-259F-4A6A-AC2B-B85E1B658318}" /m
    ```
   - For .NET Core applications:
   ```batch
   setx CORECLR_PROFILER "{918728DD-259F-4A6A-AC2B-B85E1B658318}" /m
   ```
3. Set the "service name" that better describes your application:
   ```batch
   setx OTEL_SERVICE_NAME MyServiceName /m
   ```
4. Set the endpoint of a Smart Agent or OpenTelemetry Collector that will forward
the trace data:
   ```batch
   setx OTEL_TRACE_AGENT_URL http://localhost:9080/v1/trace /m
   ```
5. Optionally, enable trace injection in logs:
   ```batch
   setx OTEL_LOGS_INJECTION true /m
   ```
6. Optionally, if instrumenting IIS applications add the following environmet variable set to `true`:
    ```batch
    setx OTEL_TRACE_DOMAIN_NEUTRAL_INSTRUMENTATION true /m
    ```
7. Enable instrumentation for the targeted application by setting
the appropriate __CLR enable profiling__ environment variable.
You can enable instrumentation at these levels:
 - For current command session
 - For a specific Windows Service
 - For a specific user
The follow snippet describes how to enable instrumentation for
the current command session according to the .NET runtime.
To enable instrumentation at different levels, see
[this](#enable-instrumentation-at-different-levels) section.
   - For .NET Framework applications:
   ```batch
   set COR_ENABLE_PROFILING=1
   ```
   - For .NET Core applications:
   ```batch
   set CORECLR_ENABLE_PROFILING=1
   ```
8. Restart your application ensuring that all environment variables above are properly
configured. If you need to check the environment variables for a process use a tool
like [Process Explorer](https://docs.microsoft.com/en-us/sysinternals/downloads/process-explorer).

#### Enable instrumentation at different levels

Enable instrumentation for a specific Windows service:
   - For .NET Framework applications:
   ```batch
   reg add HKLM\SYSTEM\CurrentControlSet\Services\<ServiceName>\Environment /v COR_ENABLE_PROFILING /d 1
   ```
   - For .NET Core applications:
   ```batch
   reg add HKLM\SYSTEM\CurrentControlSet\Services\<ServiceName>\Environment /v CORECLR_ENABLE_PROFILING /d 1
   ```

Enable instrumentation for a specific user:
   - For .NET Framework applications:
   ```batch
   setx /s %COMPUTERNAME% /u <[domain/]user> COR_ENABLE_PROFILING 1
   ```
   - For .NET Core applications:
   ```batch
   setx /s %COMPUTERNAME% /u <[domain/]user> CORECLR_ENABLE_PROFILING 1
   ```

## Configure custom instrumentation

You can build upon the provided tracing functionality by modifying and adding
to automatically generated traces. The OpenTelemetry Tracing library for .NET
provides and registers an [OpenTracing-compatible](https://github.com/opentracing/opentracing-csharp)
global tracer you can use.

OpenTracing versions 0.11.0+ are supported and the provided tracer offers a
complete implementation of the OpenTracing API.

The auto-instrumentation provides a base you can build on by adding your own
custom instrumentation. By using both instrumentation approaches, you'll be
able to present a more detailed representation of the logic and functionality
of your application, clients, and framework.

1. Add the OpenTracing dependency to your project:
    ```xml
    <PackageReference Include="OpenTracing" Version="0.12.0" />
    ```
2. Obtain the `OpenTracing.Util.GlobalTracer` instance and create spans that
automatically become child spans of any existing spans in the same context:
    ```csharp
    using OpenTracing;
    using OpenTracing.Util;

    namespace MyProject
    {
        public class MyClass
        {
            public static async void MyMethod()
            {
                // Obtain the automatically registered OpenTracing.Util.GlobalTracer instance
                var tracer = GlobalTracer.Instance;

                // Create an active span that will be automatically parented by any existing span in this context
                using (IScope scope = tracer.BuildSpan("MyTracedFunctionality").StartActive(finishSpanOnDispose: true))
                {
                    var span = scope.Span;
                    span.SetTag("MyImportantTag", "MyImportantValue");
                    span.Log("My Important Log Statement");

                    var ret = await MyAppFunctionality();

                    span.SetTag("FunctionalityReturned", ret.ToString());
                }
            }
        }
    }
    ```

## Troubleshooting

Check if you are not hitting one of the issues listed below.

### IIS applications not instrumenting expected services

Set the environment variable `OTEL_TRACE_DOMAIN_NEUTRAL_INSTRUMENTATION` to `true` - without it
the CLR profiler can't instrument many libraries/frameworks under IIS.

### Linux instrumentation not working

The proper binary needs to be selected when deploying to Linux, eg.: the default Microsoft .NET images are
based on Debian and should use the `deb` package, see the [Linux](#Linux) setup section.

If you are not sure what is the Linux distribution being used try the following commands:
```terminal
$ lsb_release -a
$ cat /etc/*release
$ cat /etc/issue*
$ cat /proc/version
```

### High CPU usage

The default installation of auto-instrumentation enables tracing all .NET processes on the box.
In the typical scenarios (dedicated VMs or containers), this is not a problem.
Use the environment variables `OTEL_PROFILER_EXCLUDE_PROCESSES` and `OTEL_PROFILER_PROCESSES`
to include/exclude applications from the tracing auto-instrumentation.
These are ";" delimited lists that control the inclusion/exclusion of processes.

### Custom instrumentation not being captured

If the code accessing `GlobalTracer.Instance` executes before any auto-instrumentation is injected
into the process the call to `GlobalTracer.Instance` will return the OpenTracing No-Operation tracer.
In this case it is necessary to force the injection of the OpenTelemetry tracer by running a method like the one below
before accessing `GlobalTracer.Instance`.

```c#
        static void InitTracer()
        {
            try
            {
                Assembly tracingAssembly = Assembly.Load(new AssemblyName("OpenTelemetry.AutoInstrumentation, Culture=neutral, PublicKeyToken=34b8972644a12429"));
                Type tracerType = tracingAssembly.GetType("OpenTelemetry.AutoInstrumentation.Tracer");

                PropertyInfo tracerInstanceProperty = tracerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                object tracerInstance = tracerInstanceProperty.GetValue(null);
            }
            catch (Exception ex)
            {
                // TODO: Replace Console.WriteLine with proper log of the application.
                Console.WriteLine("Unable to load SOpenTelemetry.AutoInstrumentation.Tracer library. Exception: {0}", ex);
            }
        }
```

### Investigating other issues

If none of the suggestions above solves your issue, detailed logs are necessary.
Follow the steps below to get the detailed logs from OpenTelemetry AutoInstrumentation for .NET:

Set the environment variable `OTEL_TRACE_DEBUG` to `true` before the instrumented process starts.
By default, the library writes the log files under the below predefined locations.
If needed, change the default location by updating the environment variable `OTEL_TRACE_LOG_PATH` to an appropriate path. 
On Linux, the default log location is `/var/log/opentelemetry/dotnet/`
On Windows, the default log location is `%ProgramData%\\OpenTelemetry .NET AutoInstrumentation\logs\`
Compress the whole folder to capture the multiple log files and send the compressed folder to us.
After obtaining the logs, remember to remove the environment variable `OTEL_TRACE_DEBUG` to avoid unnecessary overhead.

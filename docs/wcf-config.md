# WCF Instrumentation Configuration

⚠️ **NOTICE:** WCF Instrumentation has only been tested for Http and NetTcp bindings.

## WCF Client Configuration (.NET Framework)

Example project available in
[test/test-applications/integrations/TestApplication.Wcf.Client.NetFramework](../test/test-applications/integrations/TestApplication.Wcf.Client.NetFramework/)
folder.

⚠️ **NOTICE:** Instrumentation of [APM-style](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/asynchronous-programming-model-apm) calls
is supported, but have known limitations.
It is recommended to convert them to [TAP-style](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap) calls.

## WCF Client Configuration (.NET)

Instrumentation for WCF Client on .NET is not supported.

## WCF Server Configuration (.NET Framework)

Example project available in
[test/test-applications/integrations/TestApplication.Wcf.Server.NetFramework](../test/test-applications/integrations/TestApplication.Wcf.Server.NetFramework/)
folder.

## References

* [OpenTelemetry Project](https://opentelemetry.io/)
* [OpenTelemetry Contrib WCF docs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Instrumentation.Wcf-1.0.0-rc.8/src/OpenTelemetry.Instrumentation.Wcf/README.md)

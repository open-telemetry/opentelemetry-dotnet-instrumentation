# WCF Instrumentation Configuration

⚠️ **NOTICE:** WCF Instrumentation has only been tested for Http and NetTcp bindings.

## WCF Client Configuration (.NET Framework)

Example project available in
[test/test-applications/integrations/TestApplication.Wcf.Client.NetFramework](../test/test-applications/integrations/TestApplication.Wcf.Client.NetFramework/)
folder.

## WCF Client Configuration (.NET)

Add `OpenTelemetry.Instrumentation.Wcf` and `System.Diagnostics.DiagnosticSource`
package to the project. The version of `OpenTelemetry.Instrumentation.Wcf`
should match the one used by AutoInstrumentation library to avoid compatibility
issues.

```xml
  <ItemGroup>
    <PackageReference Include="System.Diagnostics.DiagnosticSource" Version="7.0.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Wcf" Version="1.0.0-rc.9" ExcludeAssets="runtime" />
  </ItemGroup>
```

Add the `IClientMessageInspector` instrumentation as an endpoint behavior on the
clients you want to instrument:

```csharp
StatusServiceClient client = new StatusServiceClient(binding, remoteAddress);
client.Endpoint.EndpointBehaviors.Add(new TelemetryEndpointBehavior());
```

Example project available in
[test/test-applications/integrations/TestApplication.Wcf.Client.DotNet](../test/test-applications/integrations/TestApplication.Wcf.Client.DotNet/)
folder.

## WCF Server Configuration (.NET Framework)

Example project available in
[test/test-applications/integrations/TestApplication.Wcf.Server.NetFramework](../test/test-applications/integrations/TestApplication.Wcf.Server.NetFramework/)
folder.

## References

* [OpenTelemetry Project](https://opentelemetry.io/)
* [OpenTelemetry Contrib WCF docs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/Instrumentation.Wcf-1.0.0-rc.8/src/OpenTelemetry.Instrumentation.Wcf/README.md)

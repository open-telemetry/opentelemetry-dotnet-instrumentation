# WCF Instrumentation Configration

## WCF Client Configuration (.NET Framework)

Add the `IClientMessageInspector` instrumentation via a behavior extension on
the clients you want to instrument:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <system.serviceModel>
    <extensions>
      <behaviorExtensions>
        <add name="telemetryExtension" type="OpenTelemetry.Instrumentation.Wcf.TelemetryEndpointBehaviorExtensionElement, OpenTelemetry.Instrumentation.Wcf" />
      </behaviorExtensions>
    </extensions>
    <behaviors>
      <endpointBehaviors>
        <behavior name="telemetry">
          <telemetryExtension />
        </behavior>
      </endpointBehaviors>
    </behaviors>
    <bindings>
      <basicHttpBinding>
        <binding name="basicHttpConfig">
          <security mode="None" />
        </binding>
      </basicHttpBinding>
      <netTcpBinding>
        <binding name="netTCPConfig">
          <security mode="None" />
        </binding>
      </netTcpBinding>
    </bindings>
    <client>
      <endpoint address="http://localhost:9009/Telemetry" binding="basicHttpBinding" bindingConfiguration="basicHttpConfig" behaviorConfiguration="telemetry" contract="TestApplication.Wcf.Shared.IStatusServiceContract" name="StatusService_Http" />
    </client>
  </system.serviceModel>
</configuration>
```

Example project available in
[test/test-applications/integrations/TestApplication.Wcf.Client.NetFramework](../test/test-applications/integrations/TestApplication.Wcf.Client.NetFramework/)
folder.

## WCF Client Configuration (.NET Core)

Add the `IClientMessageInspector` instrumentation as an endpoint behavior on the
clients you want to instrument:

```csharp
StatusServiceClient client = new StatusServiceClient(binding, remoteAddress);
client.Endpoint.EndpointBehaviors.Add(new TelemetryEndpointBehavior());
```

Example project available in
[test/test-applications/integrations/TestApplication.Wcf.Client.Core](../test/test-applications/integrations/TestApplication.Wcf.Client.Core/)
folder.

## WCF Server Configuration (.NET Framework)

### Option 1: Instrument by endpoint

To add the `IDispatchMessageInspector` instrumentation to select endpoints of a
service, use the endpoint behavior extension on the service endpoints you want
to instrument:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <system.serviceModel>
    <extensions>
      <behaviorExtensions>
        <add name="telemetryExtension" type="OpenTelemetry.Instrumentation.Wcf.TelemetryEndpointBehaviorExtensionElement, OpenTelemetry.Instrumentation.Wcf" />
      </behaviorExtensions>
    </extensions>
    <behaviors>
      <endpointBehaviors>
        <behavior name="telemetry">
          <telemetryExtension />
        </behavior>
      </endpointBehaviors>
    </behaviors>
    <bindings>
      <netTcpBinding>
        <binding name="netTCPConfig">
          <security mode="None" />
        </binding>
      </netTcpBinding>
    </bindings>
    <services>
      <service>
        <endpoint binding="netTcpBinding" bindingConfiguration="netTCPConfig" behaviorConfiguration="telemetry" contract="TestApplication.Wcf.Shared.IStatusServiceContract" />
        <host>
          <baseAddresses>
            <add baseAddress="net.tcp://localhost:9090/Telemetry" />
          </baseAddresses>
        </host>
      </service>
    </services>
  </system.serviceModel>
</configuration>
```

Example project available in
[test/test-applications/integrations/TestApplication.Wcf.Server.NetFramework](../test/test-applications/integrations/TestApplication.Wcf.Server.NetFramework/)
folder.

### Option 2: Instrument by service

To add the `IDispatchMessageInspector` instrumentation for all endpoints of a
service, use the service behavior extension on the services you want to
instrument:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <system.serviceModel>
    <extensions>
      <behaviorExtensions>
        <add name="telemetryExtension" type="OpenTelemetry.Instrumentation.Wcf.TelemetryServiceBehaviorExtensionElement, OpenTelemetry.Instrumentation.Wcf" />
      </behaviorExtensions>
    </extensions>
    <behaviors>
      <serviceBehaviors>
        <behavior name="telemetry">
          <telemetryExtension />
        </behavior>
      </serviceBehaviors>
    </behaviors>
    <bindings>
      <netTcpBinding>
        <binding name="netTCPConfig">
          <security mode="None" />
        </binding>
      </netTcpBinding>
    </bindings>
    <services>
      <service name="TestApplication.Wcf.Server.NetFramework.StatusService" behaviorConfiguration="telemetry">
        <endpoint binding="netTcpBinding" bindingConfiguration="netTCPConfig" contract="TestApplication.Wcf.Shared.IStatusServiceContract" />
        <host>
          <baseAddresses>
            <add baseAddress="net.tcp://localhost:9090/Telemetry" />
          </baseAddresses>
        </host>
      </service>
    </services>
  </system.serviceModel>
</configuration>
```

## WCF Programmatic Configuration Server and/or Client (.NET Framework + .NET Core)

To add `IDispatchMessageInspector` for servers (.NET Framework only) and/or
`IClientMessageInspector` for clients (.NET Framework & .NET Core)
programmatically, use the `TelemetryContractBehaviorAttribute` on the service
contracts you want to instrument:

```csharp
    [ServiceContract(
        Namespace = "http://opentelemetry.io/",
        Name = "StatusService",
        SessionMode = SessionMode.Allowed)]
    [TelemetryContractBehavior]
    public interface IStatusServiceContract
    {
        [OperationContract]
        Task<StatusResponse> PingAsync(StatusRequest request);
    }
```

## References

* [OpenTelemetry Project](https://opentelemetry.io/)
* [OpenTelemetry Contrib WCF docs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Wcf/README.md)

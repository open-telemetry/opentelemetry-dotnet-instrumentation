﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the InstrumentationDefinitionsGenerator tool. To safely
//     modify this file, edit InstrumentMethodAttribute on the classes and
//     compile project.

//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated. 
// </auto-generated>
//------------------------------------------------------------------------------

using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation;

internal static partial class InstrumentationDefinitions
{
    private static readonly string AssemblyFullName = typeof(InstrumentationDefinitions).Assembly.FullName!;

    private static NativeCallTargetDefinition[] GetDefinitionsArray()
    {
        var nativeCallTargetDefinitions = new List<NativeCallTargetDefinition>(10);
        // Traces
        var tracerSettings = Instrumentation.TracerSettings.Value;
        if (tracerSettings.TracesEnabled)
{
            // AspNet
            if (tracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.AspNet))
            {
                nativeCallTargetDefinitions.Add(new("System.Web", "System.Web.Compilation.BuildManager", "InvokePreStartInitMethodsCore", new[] {"System.Void", "System.Collections.Generic.ICollection`1[System.Reflection.MethodInfo]", "System.Func`1[System.IDisposable]"}, 4, 0, 0, 4, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.AspNet.HttpModuleIntegration"));
            }

            // MongoDB
            if (tracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.MongoDB))
            {
                nativeCallTargetDefinitions.Add(new("MongoDB.Driver", "MongoDB.Driver.MongoClient", ".ctor", new[] {"System.Void", "MongoDB.Driver.MongoClientSettings"}, 2, 13, 3, 2, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.MongoClientIntegration"));
            }

            // NServiceBus
            if (tracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.NServiceBus))
            {
                nativeCallTargetDefinitions.Add(new("NServiceBus.Core", "NServiceBus.EndpointConfiguration", ".ctor", new[] {"System.Void", "System.String"}, 8, 0, 0, 8, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.NServiceBus.EndpointConfigurationIntegration"));
            }

            // WcfClient
            if (tracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.WcfClient))
            {
                nativeCallTargetDefinitions.Add(new("System.ServiceModel", "System.ServiceModel.Channels.ServiceChannelProxy", "Invoke", new[] {"System.Runtime.Remoting.Messaging.IMessage", "System.Runtime.Remoting.Messaging.IMessage"}, 4, 0, 0, 4, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client.InvokeServiceChannelProxyIntegration"));
                nativeCallTargetDefinitions.Add(new("System.ServiceModel", "System.ServiceModel.ChannelFactory", "InitializeEndpoint", new[] {"System.Void", "System.String", "System.ServiceModel.EndpointAddress"}, 4, 0, 0, 4, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client.WcfClientIntegration"));
                nativeCallTargetDefinitions.Add(new("System.ServiceModel", "System.ServiceModel.ChannelFactory", "InitializeEndpoint", new[] {"System.Void", "System.String", "System.ServiceModel.EndpointAddress", "System.Configuration.Configuration"}, 4, 0, 0, 4, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client.WcfClientIntegration"));
                nativeCallTargetDefinitions.Add(new("System.ServiceModel", "System.ServiceModel.ChannelFactory", "InitializeEndpoint", new[] {"System.Void", "System.ServiceModel.Description.ServiceEndpoint"}, 4, 0, 0, 4, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client.WcfClientIntegration"));
                nativeCallTargetDefinitions.Add(new("System.ServiceModel", "System.ServiceModel.ChannelFactory", "InitializeEndpoint", new[] {"System.Void", "System.ServiceModel.Channels.Binding", "System.ServiceModel.EndpointAddress"}, 4, 0, 0, 4, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client.WcfClientIntegration"));
            }

            // WcfService
            if (tracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.WcfService))
            {
                nativeCallTargetDefinitions.Add(new("System.ServiceModel", "System.ServiceModel.ServiceHostBase", "InitializeDescription", new[] {"System.Void", "System.ServiceModel.UriSchemeKeyedCollection"}, 4, 0, 0, 4, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Service.ServiceHostIntegration"));
            }

        }

        // Metrics
        var metricSettings = Instrumentation.MetricSettings.Value;
        if (metricSettings.MetricsEnabled)
{
            // NServiceBus
            if (metricSettings.EnabledInstrumentations.Contains(MetricInstrumentation.NServiceBus))
            {
                nativeCallTargetDefinitions.Add(new("NServiceBus.Core", "NServiceBus.EndpointConfiguration", ".ctor", new[] {"System.Void", "System.String"}, 8, 0, 0, 8, 65535, 65535, AssemblyFullName, "OpenTelemetry.AutoInstrumentation.Instrumentations.NServiceBus.EndpointConfigurationIntegration"));
            }

        }

        return nativeCallTargetDefinitions.ToArray();
    }
}

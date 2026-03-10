// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client;

/// <summary>
/// ChannelFactory instrumentation.
/// </summary>
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelAssemblyName,
    typeName: WcfClientConstants.ChannelFactoryTypeName,
    methodName: WcfClientConstants.InitializeEndpointMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [ClrNames.String, WcfClientConstants.EndpointAddressTypeName],
    minimumVersion: WcfCommonConstants.Min4Version,
    maximumVersion: WcfCommonConstants.Max4Version,
    integrationName: WcfClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelAssemblyName,
    typeName: WcfClientConstants.ChannelFactoryTypeName,
    methodName: WcfClientConstants.InitializeEndpointMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [WcfClientConstants.ServiceEndpointTypeName],
    minimumVersion: WcfCommonConstants.Min4Version,
    maximumVersion: WcfCommonConstants.Max4Version,
    integrationName: WcfClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelAssemblyName,
    typeName: WcfClientConstants.ChannelFactoryTypeName,
    methodName: WcfClientConstants.InitializeEndpointMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [WcfClientConstants.BindingTypeName, WcfClientConstants.EndpointAddressTypeName],
    minimumVersion: WcfCommonConstants.Min4Version,
    maximumVersion: WcfCommonConstants.Max4Version,
    integrationName: WcfClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
#if NET
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelPrimitivesAssemblyName,
    typeName: WcfClientConstants.ChannelFactoryTypeName,
    methodName: WcfClientConstants.InitializeEndpointMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [ClrNames.String, WcfClientConstants.EndpointAddressTypeName],
    minimumVersion: WcfCommonConstants.Min6Version,
    maximumVersion: WcfCommonConstants.Max10Version,
    integrationName: WcfClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelPrimitivesAssemblyName,
    typeName: WcfClientConstants.ChannelFactoryTypeName,
    methodName: WcfClientConstants.InitializeEndpointMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [WcfClientConstants.ServiceEndpointTypeName],
    minimumVersion: WcfCommonConstants.Min6Version,
    maximumVersion: WcfCommonConstants.Max10Version,
    integrationName: WcfClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelPrimitivesAssemblyName,
    typeName: WcfClientConstants.ChannelFactoryTypeName,
    methodName: WcfClientConstants.InitializeEndpointMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [WcfClientConstants.BindingTypeName, WcfClientConstants.EndpointAddressTypeName],
    minimumVersion: WcfCommonConstants.Min6Version,
    maximumVersion: WcfCommonConstants.Max10Version,
    integrationName: WcfClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
#endif
#if NETFRAMEWORK
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelAssemblyName,
    typeName: WcfClientConstants.ChannelFactoryTypeName,
    methodName: WcfClientConstants.InitializeEndpointMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [ClrNames.String, WcfClientConstants.EndpointAddressTypeName, WcfClientConstants.ConfigurationTypeName],
    minimumVersion: WcfCommonConstants.Min4Version,
    maximumVersion: WcfCommonConstants.Max4Version,
    integrationName: WcfClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
#endif
public static class WcfClientIntegration
{
    /// <summary>
    /// OnMethodEnd callback
    /// </summary>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="exception">Exception value</param>
    /// <param name="state">CallTarget state</param>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <returns>A response value, in an async scenario will be T of Task of T</returns>
    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, in CallTargetState state)
    where TTarget : WcfClientInitializer.IChannelFactory
    {
        WcfClientInitializer.Initialize(instance);

        return CallTargetReturn.GetDefault();
    }
}

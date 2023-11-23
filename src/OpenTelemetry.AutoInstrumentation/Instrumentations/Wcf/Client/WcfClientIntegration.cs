// <copyright file="WcfClientIntegration.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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
    parameterTypeNames: new[] { ClrNames.String, WcfClientConstants.EndpointAddressTypeName },
    minimumVersion: WcfCommonConstants.Min4Version,
    maximumVersion: WcfCommonConstants.Max4Version,
    integrationName: WcfClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelAssemblyName,
    typeName: WcfClientConstants.ChannelFactoryTypeName,
    methodName: WcfClientConstants.InitializeEndpointMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { WcfClientConstants.ServiceEndpointTypeName },
    minimumVersion: WcfCommonConstants.Min4Version,
    maximumVersion: WcfCommonConstants.Max4Version,
    integrationName: WcfClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelAssemblyName,
    typeName: WcfClientConstants.ChannelFactoryTypeName,
    methodName: WcfClientConstants.InitializeEndpointMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { WcfClientConstants.BindingTypeName, WcfClientConstants.EndpointAddressTypeName },
    minimumVersion: WcfCommonConstants.Min4Version,
    maximumVersion: WcfCommonConstants.Max4Version,
    integrationName: WcfClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
#if NET6_0_OR_GREATER
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelPrimitivesAssemblyName,
    typeName: WcfClientConstants.ChannelFactoryTypeName,
    methodName: WcfClientConstants.InitializeEndpointMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { ClrNames.String, WcfClientConstants.EndpointAddressTypeName },
    minimumVersion: WcfCommonConstants.Min6Version,
    maximumVersion: WcfCommonConstants.Max6Version,
    integrationName: WcfClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelPrimitivesAssemblyName,
    typeName: WcfClientConstants.ChannelFactoryTypeName,
    methodName: WcfClientConstants.InitializeEndpointMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { WcfClientConstants.ServiceEndpointTypeName },
    minimumVersion: WcfCommonConstants.Min6Version,
    maximumVersion: WcfCommonConstants.Max6Version,
    integrationName: WcfClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelPrimitivesAssemblyName,
    typeName: WcfClientConstants.ChannelFactoryTypeName,
    methodName: WcfClientConstants.InitializeEndpointMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { WcfClientConstants.BindingTypeName, WcfClientConstants.EndpointAddressTypeName },
    minimumVersion: WcfCommonConstants.Min6Version,
    maximumVersion: WcfCommonConstants.Max6Version,
    integrationName: WcfClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
#endif
#if NETFRAMEWORK
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelAssemblyName,
    typeName: WcfClientConstants.ChannelFactoryTypeName,
    methodName: WcfClientConstants.InitializeEndpointMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { ClrNames.String, WcfClientConstants.EndpointAddressTypeName, WcfClientConstants.ConfigurationTypeName },
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

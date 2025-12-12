// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#if NETFRAMEWORK
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Service;

/// <summary>
/// ServerHostIntegration.
/// </summary>
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelAssemblyName,
    typeName: WcfServiceConstants.ServiceHostBaseTypeName,
    methodName: WcfServiceConstants.InitializeDescriptionMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [WcfServiceConstants.UriSchemeKeyedCollectionTypeName],
    minimumVersion: WcfCommonConstants.Min4Version,
    maximumVersion: WcfCommonConstants.Max4Version,
    integrationName: WcfServiceConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class ServiceHostIntegration
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
    where TTarget : WcfServiceInitializer.IServiceHostBase
    {
        WcfServiceInitializer.Initialize(instance);

        return CallTargetReturn.GetDefault();
    }
}
#endif

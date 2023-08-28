// <copyright file="ServiceHostIntegration.cs" company="OpenTelemetry Authors">
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
    parameterTypeNames: new[] { WcfServiceConstants.UriSchemeKeyedCollectionTypeName },
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

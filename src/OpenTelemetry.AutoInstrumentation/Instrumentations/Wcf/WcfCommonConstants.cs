// <copyright file="WcfCommonConstants.cs" company="OpenTelemetry Authors">
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
namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf;

internal static class WcfCommonConstants
{
#if NETFRAMEWORK
    public const string ServiceModelAssemblyName = "System.ServiceModel";
#else
    public const string ServiceModelAssemblyName = "System.Private.ServiceModel";

    public const string ServiceModelPrimitivesAssemblyName = "System.ServiceModel.Primitives";
    public const string Min6Version = "6.0.0";
    public const string Max6Version = "6.*.*";
#endif
    public const string Min4Version = "4.0.0";
    public const string Max4Version = "4.*.*";
}

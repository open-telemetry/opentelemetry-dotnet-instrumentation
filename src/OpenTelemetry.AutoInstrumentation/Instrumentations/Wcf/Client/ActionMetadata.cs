// <copyright file="ActionMetadata.cs" company="OpenTelemetry Authors">
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

// source originated from: https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/06b9a286a6ab2af5257ce26b5dcb6fac56112f96/src/OpenTelemetry.Instrumentation.Wcf
#if NETFRAMEWORK
namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client;
internal sealed class ActionMetadata
{
    public ActionMetadata(string? contractName, string operationName)
    {
        ContractName = contractName;
        OperationName = operationName;
    }

    public string? ContractName { get; }

    public string OperationName { get; }
}
#endif

// <copyright file="GraphQLExecuteAsyncAttribute.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.GraphQL;

internal class GraphQLExecuteAsyncAttribute : InstrumentMethodAttribute
{
    public GraphQLExecuteAsyncAttribute(string assemblyName, string typeName, string minimumVersion, string maximumVersion)
        : base(
            assemblyName: assemblyName,
            typeName: typeName,
            methodName: "ExecuteAsync",
            returnTypeName: "System.Threading.Tasks.Task`1[GraphQL.ExecutionResult]",
            parameterTypeNames: new[] { "GraphQL.Execution.ExecutionContext" },
            minimumVersion: minimumVersion,
            maximumVersion: maximumVersion,
            integrationName: GraphQLCommon.IntegrationName,
            type: InstrumentationType.Trace)
    {
    }
}

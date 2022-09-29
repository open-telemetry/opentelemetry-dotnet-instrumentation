// <copyright file="OperationTypeProxy.cs" company="OpenTelemetry Authors">
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

/// <summary>
/// A proxy enum for GraphQL.Language.AST.OperationType.
/// The enum values must match those of GraphQL.Language.AST.OperationType for spans
/// to be decorated with the correct operation. Since the original type is public,
/// we not expect changes between minor versions of the GraphQL library.
/// </summary>
public enum OperationTypeProxy
{
    /// <summary>
    /// A query operation.
    /// </summary>
    Query,

    /// <summary>
    /// A mutation operation.
    /// </summary>
    Mutation,

    /// <summary>
    /// A subscription operation.
    /// </summary>
    Subscription
}

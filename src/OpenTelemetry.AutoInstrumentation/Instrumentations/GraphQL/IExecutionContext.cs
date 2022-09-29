// <copyright file="IExecutionContext.cs" company="OpenTelemetry Authors">
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
/// GraphQL.Execution.ExecutionContext interface for ducktyping
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// Gets the document associated with the execution context
    /// </summary>
    IDocument Document { get; }

    /// <summary>
    /// Gets the operation associated with the execution context
    /// </summary>
    IOperation Operation { get; }

    /// <summary>
    /// Gets the execution errors
    /// </summary>
    IExecutionErrors Errors { get; }
}

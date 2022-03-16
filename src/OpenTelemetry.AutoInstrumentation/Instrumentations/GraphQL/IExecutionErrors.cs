// <copyright file="IExecutionErrors.cs" company="OpenTelemetry Authors">
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
/// GraphQL.ExecutionErrors interface for ducktyping
/// </summary>
public interface IExecutionErrors
{
    /// <summary>
    /// Gets the number of errors
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the ExecutionError at the specified index
    /// </summary>
    /// <param name="index">Index to lookup</param>
    /// <returns>An execution error</returns>
    IExecutionError this[int index] { get; }
}

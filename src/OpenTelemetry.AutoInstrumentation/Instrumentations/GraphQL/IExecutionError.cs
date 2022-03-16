// <copyright file="IExecutionError.cs" company="OpenTelemetry Authors">
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

using System.Collections.Generic;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.GraphQL;

/// <summary>
/// GraphQL.ExecutionError interface for ducktyping
/// </summary>
public interface IExecutionError
{
    /// <summary>
    /// Gets a code for the error
    /// </summary>
    string Code { get; }

    /// <summary>
    /// Gets a list of locations in the document where the error applies
    /// </summary>
    IEnumerable<object> Locations { get; }

    /// <summary>
    /// Gets a message for the error
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets the path in the document where the error applies
    /// </summary>
    IEnumerable<string> Path { get; }
}

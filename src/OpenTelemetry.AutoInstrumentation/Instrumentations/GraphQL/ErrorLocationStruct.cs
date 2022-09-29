// <copyright file="ErrorLocationStruct.cs" company="OpenTelemetry Authors">
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

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.GraphQL;

/// <summary>
/// GraphQL.ErrorLocation interface for ducktyping
/// </summary>
[DuckCopy]
public struct ErrorLocationStruct
{
    /// <summary>
    /// Gets the line number of the document where the error occurred
    /// </summary>
    public int Line;

    /// <summary>
    /// Gets the column number of the document where the error occurred
    /// </summary>
    public int Column;
}

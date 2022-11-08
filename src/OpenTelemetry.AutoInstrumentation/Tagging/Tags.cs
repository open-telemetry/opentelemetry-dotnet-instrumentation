// <copyright file="Tags.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Tagging;

/// <summary>
/// Standard span tags used by integrations.
/// </summary>
internal static class Tags
{
    /// <summary>
    /// The hostname of a outgoing server connection.
    /// </summary>
    public const string OutHost = "out.host";

    /// <summary>
    /// The port of a outgoing server connection.
    /// </summary>
    public const string OutPort = "out.port";

    /// <summary>
    /// GraphQL specific tags
    /// </summary>
    public static class GraphQL
    {
        /// <summary>
        /// The name of the operation being executed.
        /// </summary>
        public const string OperationName = "graphql.operation.name";

        /// <summary>
        /// The type of the operation being executed.
        /// </summary>
        public const string OperationType = "graphql.operation.type";

        /// <summary>
        /// The GraphQL document being executed.
        /// </summary>
        public const string Document = "graphql.document";
    }
}

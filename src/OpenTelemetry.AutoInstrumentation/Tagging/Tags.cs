// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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

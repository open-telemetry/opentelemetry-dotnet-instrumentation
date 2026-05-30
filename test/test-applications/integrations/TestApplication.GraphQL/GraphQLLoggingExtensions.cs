// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.GraphQL;

internal static partial class GraphQLLoggingExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "{key} = {value}")]
    public static partial void LogEnvironmentVariable(this ILogger logger, string key, string value);
}

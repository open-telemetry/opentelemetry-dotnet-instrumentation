// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry logging category options.
/// </summary>
public sealed class OpenTelemetryLoggingCategoryOptions
{
    internal OpenTelemetryLoggingCategoryOptions(
        string categoryPrefix,
        string logLevel)
    {
        Debug.Assert(!string.IsNullOrEmpty(categoryPrefix));
        Debug.Assert(!string.IsNullOrEmpty(logLevel));

        CategoryPrefix = categoryPrefix;
        LogLevel = logLevel;
    }

    /// <summary>
    /// Gets the log category prefix to listen to.
    /// </summary>
    public string CategoryPrefix { get; }

    /// <summary>
    /// Gets the log category log level to listen to.
    /// </summary>
    public string LogLevel { get; }
}

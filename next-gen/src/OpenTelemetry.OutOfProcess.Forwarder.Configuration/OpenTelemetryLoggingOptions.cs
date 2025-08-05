// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry logging options.
/// </summary>
public sealed class OpenTelemetryLoggingOptions
{
    internal static OpenTelemetryLoggingOptions ParseFromConfig(IConfigurationSection config)
    {
        Debug.Assert(config != null);

        List<OpenTelemetryLoggingCategoryOptions> categories = new();
        string? defaultLogLevel = null;

        foreach (KeyValuePair<string, string?> category in config.GetSection("Categories").AsEnumerable(makePathsRelative: true))
        {
            if (string.IsNullOrEmpty(category.Key)
                || string.IsNullOrEmpty(category.Value))
            {
                continue;
            }

            if (string.Equals("Default", category.Key, StringComparison.OrdinalIgnoreCase))
            {
                defaultLogLevel = category.Value;
                continue;
            }

            categories.Add(new(category.Key, category.Value));
        }

        return new(
            defaultLogLevel,
            config.GetValueOrUseDefault("IncludeScopes", false),
            categories,
            OpenTelemetryBatchOptions.ParseFromConfig(config.GetSection("Batch")));
    }

    internal OpenTelemetryLoggingOptions(
        string? defaultLogLevel,
        bool includeScopes,
        IReadOnlyCollection<OpenTelemetryLoggingCategoryOptions> categoryOptions,
        OpenTelemetryBatchOptions batchOptions)
    {
        Debug.Assert(categoryOptions != null);
        Debug.Assert(batchOptions != null);

        DefaultLogLevel = defaultLogLevel;
        IncludeScopes = includeScopes;
        CategoryOptions = categoryOptions;
        BatchOptions = batchOptions;
    }

    /// <summary>
    /// Gets the default log level to listen to.
    /// </summary>
    public string? DefaultLogLevel { get; }

    /// <summary>
    /// Gets a value indicating whether or not to include scopes in logging.
    /// </summary>
    public bool IncludeScopes { get; }

    /// <summary>
    /// Gets the logging category options.
    /// </summary>
    public IReadOnlyCollection<OpenTelemetryLoggingCategoryOptions> CategoryOptions { get; }

    /// <summary>
    /// Gets the logging batch options.
    /// </summary>
    public OpenTelemetryBatchOptions BatchOptions { get; }
}

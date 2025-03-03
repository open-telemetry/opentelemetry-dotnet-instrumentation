// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry;

/// <summary>
/// Stores details about an instrumentation scope.
/// </summary>
public sealed record InstrumentationScope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InstrumentationScope"/> class.
    /// </summary>
    /// <param name="name">Instrumentation scope name.</param>
    public InstrumentationScope(
        string name)
    {
        Name = name ?? string.Empty;
    }

    /// <summary>
    /// Gets the instrumentation scope name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the instrumentation scope version.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets the instrumentation scope attributes.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, object?>>? Attributes
    {
        get => field;
        init
        {
            if (value != null)
            {
                var tagList = new List<KeyValuePair<string, object?>>(value);
                tagList.Sort((left, right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));
                field = tagList.AsReadOnly();
            }
            else
            {
                field = null;
            }
        }
    }
}

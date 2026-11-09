// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Controls how process environment trace context is applied to root activities.
/// </summary>
internal enum ProcessContextExtractionMode
{
    /// <summary>
    /// Use the extracted context as the remote parent.
    /// </summary>
    Parent,

    /// <summary>
    /// Link the activity to the extracted context.
    /// </summary>
    Link,

    /// <summary>
    /// Do not extract context from the process environment.
    /// </summary>
    Off
}

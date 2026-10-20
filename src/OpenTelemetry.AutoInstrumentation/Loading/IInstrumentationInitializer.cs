// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Loading;

internal interface IInstrumentationInitializer
{
    /// <summary>
    /// Registers a fully constructed initializer. Registration may initialize instrumentation immediately.
    /// </summary>
    /// <param name="lazyInstrumentationLoader">The loader that detects the required assemblies.</param>
    void Register(LazyInstrumentationLoader lazyInstrumentationLoader);
}

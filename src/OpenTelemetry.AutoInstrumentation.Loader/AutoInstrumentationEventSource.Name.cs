// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Tracing;

// ReSharper disable once CheckNamespace
namespace OpenTelemetry.AutoInstrumentation;

[EventSource(Name = "OpenTelemetry-AutoInstrumentation-Loader")]
internal partial class AutoInstrumentationEventSource
{
}

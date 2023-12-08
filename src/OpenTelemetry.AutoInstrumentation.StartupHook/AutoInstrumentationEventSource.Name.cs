// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Tracing;

namespace OpenTelemetry.AutoInstrumentation;

[EventSource(Name = "OpenTelemetry-AutoInstrumentation-StartupHook")]
internal partial class AutoInstrumentationEventSource
{
}

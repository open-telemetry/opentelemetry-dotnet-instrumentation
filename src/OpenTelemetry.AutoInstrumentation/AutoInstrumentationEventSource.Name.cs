// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Tracing;

namespace OpenTelemetry.AutoInstrumentation;

[EventSource(Name = "OpenTelemetry-AutoInstrumentation")]
internal partial class AutoInstrumentationEventSource
{
}

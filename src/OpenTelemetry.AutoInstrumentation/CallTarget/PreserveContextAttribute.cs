// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.CallTarget;

/// <summary>
/// Apply on a calltarget async callback to indicate that the method
/// should execute under the current synchronization context/task scheduler.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class PreserveContextAttribute : Attribute
{
}

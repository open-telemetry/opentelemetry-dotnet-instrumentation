// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.DuckTyping;

/// <summary>
/// Ignores the member when DuckTyping
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
internal sealed class DuckIgnoreAttribute : Attribute
{
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.DuckTyping;

/// <summary>
/// Use to include a member that would normally be ignored
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class DuckIncludeAttribute : Attribute
{
}

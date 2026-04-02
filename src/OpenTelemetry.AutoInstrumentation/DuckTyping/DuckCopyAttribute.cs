// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.DuckTyping;

/// <summary>
/// Duck copy struct attribute
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
internal sealed class DuckCopyAttribute : Attribute
{
}

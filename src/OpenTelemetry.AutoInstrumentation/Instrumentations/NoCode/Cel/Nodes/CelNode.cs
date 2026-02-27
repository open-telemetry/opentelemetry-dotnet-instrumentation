// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Base class for CEL expression nodes in the abstract syntax tree.
/// </summary>
internal abstract class CelNode
{
    public abstract object? Evaluate(NoCodeExpressionContext context);
}

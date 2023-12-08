// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

internal abstract class Rule
{
    public string Name { get; protected set; } = string.Empty;

    public string Description { get; protected set; } = string.Empty;

    internal abstract bool Evaluate();
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Tagging;

internal interface IProperty<out TResult>
{
    bool IsReadOnly { get; }

    string Key { get; }

    Func<ITags, TResult> Getter { get; }
}

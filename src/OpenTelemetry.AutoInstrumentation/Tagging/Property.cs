// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Tagging;

internal class Property<TTags, TResult> : IProperty<TResult>
{
    public Property(string key, Func<TTags, TResult> getter)
    {
        Key = key;
        Getter = tags => getter((TTags)tags);
    }

    public virtual bool IsReadOnly => false;

    public string Key { get; }

    public Func<ITags, TResult> Getter { get; }
}

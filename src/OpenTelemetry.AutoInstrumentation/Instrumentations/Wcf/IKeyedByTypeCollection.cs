// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf;

internal interface IKeyedByTypeCollection
{
    void Add(object o);

    bool Contains(Type t);
}

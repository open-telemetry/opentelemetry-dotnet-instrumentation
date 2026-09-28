// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.DuckTypes;

// shared shape for the string-property lookups a context propagator getter needs,
// common to both IBM.XMS.IMessage and the provider-level async message shape.
internal interface IXmsPropertyContext
{
    public bool PropertyExists(string name);

    public string? GetStringProperty(string name);
}

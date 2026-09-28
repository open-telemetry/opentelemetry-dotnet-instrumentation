// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.DuckTypes;

// IBM.XMS.Client.Impl.XmsProviderMessageListener has no public path to its owning connection.
// It holds one on a private instance field, `connection_`, typed IBM.XMS.Client.Impl.XmsConnectionImpl,
// which implements IPropertyContext - exposed here as a plain object so the caller can duck-cast
// it onward to IXmsPropertyContext without a compile-time reference to IBM.XMS.
internal interface IXmsProviderMessageListener
{
    [DuckField(Name = "connection_")]
    public object? Connection { get; }
}

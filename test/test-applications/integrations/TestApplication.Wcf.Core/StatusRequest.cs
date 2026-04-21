// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.Serialization;

namespace TestApplication.Wcf.Core;

[DataContract(Namespace = "http://opentelemetry.io/")]
#pragma warning disable CA1812 // Avoid uninstantiated internal classes. It is created by WCF runtime.
internal sealed class StatusRequest
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. It is created by WCF runtime.
{
    [DataMember]
    public string Status { get; set; } = string.Empty;
}

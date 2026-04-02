// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.Serialization;

namespace TestApplication.Wcf.Server.NetFramework;

[DataContract(Namespace = "http://opentelemetry.io/")]
#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by WCF.
internal sealed class StatusRequest
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by WCF.
{
    [DataMember]
    public string Status { get; set; } = string.Empty;
}

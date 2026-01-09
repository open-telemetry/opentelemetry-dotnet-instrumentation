// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.Serialization;

namespace TestApplication.Wcf.Client.NetFramework;

[DataContract(Namespace = "http://opentelemetry.io/")]
internal sealed class StatusRequest
{
    [DataMember]
    public string Status { get; set; } = string.Empty;
}

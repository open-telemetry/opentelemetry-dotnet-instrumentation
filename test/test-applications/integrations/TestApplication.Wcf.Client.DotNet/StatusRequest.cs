// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.Serialization;

namespace TestApplication.Wcf.Client.DotNet;

[DataContract(Namespace = "http://opentelemetry.io/")]
public class StatusRequest
{
    [DataMember]
    public string? Status { get; set; }
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.Serialization;

namespace TestApplication.Wcf.Client.DotNet;

[DataContract(Namespace = "http://opentelemetry.io/")]
public class StatusResponse
{
    [DataMember]
    public DateTimeOffset ServerTime { get; set; }
}

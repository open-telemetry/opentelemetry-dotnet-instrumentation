// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.Serialization;

namespace TestApplication.Wcf.Core;

[DataContract(Namespace = "http://opentelemetry.io/")]
internal sealed class StatusResponse
{
    [DataMember]
    public DateTimeOffset ServerTime { get; set; }
}

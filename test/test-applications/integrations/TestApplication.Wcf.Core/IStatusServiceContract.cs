// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using CoreWCF;

namespace TestApplication.Wcf.Core;

[ServiceContract(Namespace = "http://opentelemetry.io/", Name = "StatusService")]
internal interface IStatusServiceContract
{
    [OperationContract]
    Task<StatusResponse> PingAsync(StatusRequest request);
}

// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ServiceModel;
using System.Threading.Tasks;

namespace TestApplication.Wcf.Client.NetFramework;

[ServiceContract(Namespace = "http://opentelemetry.io/", Name = "StatusService", SessionMode = SessionMode.Allowed)]
public interface IStatusServiceContract
{
    [OperationContract]
    Task<StatusResponse> PingAsync(StatusRequest request);
}
